import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { QueueApiService } from '../infrastructure/queue-api.service';
import { DispatchStatus } from '../models/queue.models';
import { QueueStore } from './queue.store';

@Injectable()
export class QueueFacade {
  private readonly api = inject(QueueApiService);
  private readonly store = inject(QueueStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly summary = this.store.summary;
  readonly messages = this.store.messages;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly selectedStatus = this.store.selectedStatus;
  readonly hasSummary = this.store.hasSummary;
  readonly hasMessages = this.store.hasMessages;
  readonly failedCount = this.store.failedCount;
  readonly activeCount = this.store.activeCount;

  // ── Actions ──
  loadSummary(): void {
    this.store.setLoading(true);
    this.api.getSummary().pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (summary) => this.store.setSummary(summary),
      error: () => {
        this.store.setError('Error al cargar resumen de cola');
        this.toast.error('No se pudo cargar el resumen de la cola');
      },
    });
  }

  loadByStatus(status: DispatchStatus): void {
    this.store.setSelectedStatus(status);
    this.store.setLoading(true);
    this.api.getByStatus(status).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (messages) => this.store.setMessages(messages),
      error: () => {
        this.store.setError('Error al cargar mensajes');
        this.toast.error('No se pudieron cargar los mensajes');
      },
    });
  }

  clearStatusFilter(): void {
    this.store.clearMessages();
  }

  refresh(): void {
    this.loadSummary();
    const status = this.store.selectedStatus();
    if (status) {
      this.loadByStatus(status);
    }
  }
}
