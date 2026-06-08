import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs';

import { AuthStore } from '../../../../core/auth/store/auth.store';
import { SignalRService } from '../../../../core/services/signalr.service';
import { OutboxApiService } from '../../infrastructure/outbox-api.service';
import { OutboxActivity, OutboxCategoryFilter, storeForCategory } from '../../models/outbox.models';

@Component({
  selector: 'app-outbox-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  templateUrl: './outbox-page.component.html',
  styleUrl: './outbox-page.component.scss',
})
export default class OutboxPage implements OnInit {
  private readonly api = inject(OutboxApiService);
  private readonly signalR = inject(SignalRService);
  private readonly authStore = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);

  /** Mirrors the backend: retry/dead-letter require ManageQueue. */
  protected readonly canManage = computed(() => this.authStore.hasPermission('ManageQueue'));

  protected readonly items = signal<OutboxActivity[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly category = signal<OutboxCategoryFilter>('');
  protected readonly status = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly totalCount = signal(0);

  protected readonly categories: { value: OutboxCategoryFilter; label: string }[] = [
    { value: '', label: 'Todos' },
    { value: 'NodeSync', label: 'Node Sync' },
    { value: 'Notification', label: 'Notificaciones' },
  ];

  protected readonly statuses = ['', 'Pending', 'Sent', 'Failed'];

  constructor() {
    // (Re)join the outbox group whenever the SignalR connection is up (survives reconnects).
    effect(() => {
      if (this.signalR.connected()) {
        this.signalR.joinOutbox().catch(() => undefined);
      }
    });
  }

  ngOnInit(): void {
    this.load();

    // Live refresh: the dispatcher emits one event per processed item, so debounce bursts.
    this.signalR
      .on('OutboxEntryChanged')
      .pipe(takeUntilDestroyed(this.destroyRef), debounceTime(1000))
      .subscribe(() => this.load());

    this.destroyRef.onDestroy(() => this.signalR.leaveOutbox().catch(() => undefined));
  }

  protected totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getActivity({
        category: this.category() || undefined,
        status: this.status() || undefined,
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (res) => {
          this.items.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('No se pudo cargar la actividad del outbox.');
          this.loading.set(false);
        },
      });
  }

  protected setCategory(value: OutboxCategoryFilter): void {
    this.category.set(value);
    this.page.set(1);
    this.load();
  }

  protected setStatus(value: string): void {
    this.status.set(value);
    this.page.set(1);
    this.load();
  }

  protected prevPage(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.load();
    }
  }

  protected nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
      this.load();
    }
  }

  protected retry(item: OutboxActivity): void {
    this.api.retry(storeForCategory(item.category), item.id).subscribe({ next: () => this.load() });
  }

  protected deadLetter(item: OutboxActivity): void {
    this.api.deadLetter(storeForCategory(item.category), item.id).subscribe({ next: () => this.load() });
  }

  protected statusClass(status: string): string {
    switch (status) {
      case 'Sent': return 'status status--sent';
      case 'Failed': return 'status status--failed';
      case 'Pending': return 'status status--pending';
      default: return 'status';
    }
  }
}
