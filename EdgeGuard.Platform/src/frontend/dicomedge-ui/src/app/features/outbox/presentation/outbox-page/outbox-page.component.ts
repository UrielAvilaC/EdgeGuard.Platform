import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs';
import { faSync, faRotateRight, faBan } from '@fortawesome/free-solid-svg-icons';

import { AuthStore } from '../../../../core/auth/store/auth.store';
import { SignalRService } from '../../../../core/services/signalr.service';
import { OutboxApiService } from '../../infrastructure/outbox-api.service';
import { OutboxActivity, OutboxCategoryFilter, storeForCategory } from '../../models/outbox.models';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiStatusBadge, StatusColorMap } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { PaginationMeta } from '../../../../shared/models/pagination.model';

@Component({
  selector: 'app-outbox-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    UiPageHeader,
    UiButton,
    UiIconButton,
    UiDataTable,
    UiCellDef,
    UiStatusBadge,
    UiAlert,
    UiDropdown,
    RelativeTimePipe,
  ],
  templateUrl: './outbox-page.component.html',
  styleUrl: './outbox-page.component.scss',
})
export default class OutboxPage implements OnInit {
  private readonly api = inject(OutboxApiService);
  private readonly signalR = inject(SignalRService);
  private readonly authStore = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly faSync = faSync;
  protected readonly faRotateRight = faRotateRight;
  protected readonly faBan = faBan;

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

  protected readonly pagination = computed<PaginationMeta>(() => ({
    page: this.page(),
    pageSize: this.pageSize(),
    total: this.totalCount(),
  }));

  protected readonly columns: TableColumn<OutboxActivity>[] = [
    { key: 'topicName', header: 'Topic', width: '16%' },
    { key: 'category', header: 'Categoría', width: '12%' },
    { key: 'target', header: 'Destino', width: '14%' },
    { key: 'status', header: 'Estado', width: '10%' },
    { key: 'attempts', header: 'Intentos', width: '8%', align: 'center' },
    { key: 'createdAt', header: 'Creado', width: '11%' },
    { key: 'processedAt', header: 'Procesado', width: '11%' },
    { key: 'lastError', header: 'Error', width: '14%' },
    { key: 'actions', header: '', width: '10%' },
  ];

  protected readonly statusColors: StatusColorMap = {
    sent: 'success',
    failed: 'danger',
    pending: 'warning',
  };

  protected readonly categoryOptions: DropdownOption<OutboxCategoryFilter>[] = [
    { value: '', label: 'Todas las categorías' },
    { value: 'NodeSync', label: 'Node Sync' },
    { value: 'Notification', label: 'Notificaciones' },
  ];

  protected readonly statusOptions: DropdownOption<string>[] = [
    { value: '', label: 'Todos los estados' },
    { value: 'Pending', label: 'Pending' },
    { value: 'Sent', label: 'Sent' },
    { value: 'Failed', label: 'Failed' },
  ];

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

  protected changePage(page: number, pageSize: number): void {
    this.page.set(page);
    this.pageSize.set(pageSize);
    this.load();
  }

  protected retry(item: OutboxActivity): void {
    this.api.retry(storeForCategory(item.category), item.id).subscribe({ next: () => this.load() });
  }

  protected deadLetter(item: OutboxActivity): void {
    this.api.deadLetter(storeForCategory(item.category), item.id).subscribe({ next: () => this.load() });
  }
}
