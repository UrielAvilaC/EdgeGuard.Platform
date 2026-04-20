import { computed, Injectable, signal } from '@angular/core';

import { DispatchStatus, QueueMessage, QueueSummary } from '../models/queue.models';

@Injectable()
export class QueueStore {
  // ── Private state ──
  private readonly _summary = signal<QueueSummary | null>(null);
  private readonly _messages = signal<QueueMessage[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _selectedStatus = signal<DispatchStatus | null>(null);

  // ── Public readonly ──
  readonly summary = this._summary.asReadonly();
  readonly messages = this._messages.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly selectedStatus = this._selectedStatus.asReadonly();

  // ── Computed ──
  readonly hasSummary = computed(() => this._summary() !== null);
  readonly hasMessages = computed(() => this._messages().length > 0);

  readonly failedCount = computed(() => {
    const s = this._summary();
    if (!s) return 0;
    return s.deliveryFailed + s.validationFailed;
  });

  readonly activeCount = computed(() => {
    const s = this._summary();
    if (!s) return 0;
    return s.pendingValidation + s.validated + s.routed + s.queued + s.dispatching;
  });

  // ── Mutations ──
  setSummary(summary: QueueSummary): void {
    this._summary.set(summary);
    this._error.set(null);
  }

  setMessages(messages: QueueMessage[]): void {
    this._messages.set(messages);
  }

  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setError(error: string | null): void {
    this._error.set(error);
  }

  setSelectedStatus(status: DispatchStatus | null): void {
    this._selectedStatus.set(status);
  }

  clearMessages(): void {
    this._messages.set([]);
    this._selectedStatus.set(null);
  }
}
