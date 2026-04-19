import { computed, Injectable, signal } from '@angular/core';

import {
  DashboardSummary,
  DashboardStudy,
  DashboardNode,
  QueueSummary,
  Hl7ListenerStatus,
} from '../models/dashboard.models';

const EMPTY_QUEUE: QueueSummary = {
  pendingValidation: 0,
  validated: 0,
  routed: 0,
  queued: 0,
  dispatching: 0,
  delivered: 0,
  deliveryFailed: 0,
  validationFailed: 0,
  totalInPipeline: 0,
};

@Injectable()
export class DashboardStore {
  // ── Private state ──
  private readonly _summary = signal<DashboardSummary | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _lastRefreshedAt = signal<Date | null>(null);

  // ── Public readonly ──
  readonly summary = this._summary.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly lastRefreshedAt = this._lastRefreshedAt.asReadonly();

  // ── Computed selectors ──
  readonly totalStudies = computed(() => this._summary()?.totalStudies ?? 0);
  readonly totalPatients = computed(() => this._summary()?.totalPatients ?? 0);
  readonly totalNodes = computed(() => this._summary()?.totalNodes ?? 0);
  readonly activeNodes = computed(() => this._summary()?.activeNodes ?? 0);
  readonly pendingPacsStudies = computed(() => this._summary()?.pendingPacsStudies ?? 0);
  readonly failedStudies = computed(() => this._summary()?.failedStudies ?? 0);

  readonly queueSummary = computed<QueueSummary>(() => this._summary()?.queueSummary ?? EMPTY_QUEUE);
  readonly hl7Status = computed<Hl7ListenerStatus | null>(() => this._summary()?.hl7Status ?? null);
  readonly recentStudies = computed<DashboardStudy[]>(() => this._summary()?.recentStudies ?? []);
  readonly nodes = computed<DashboardNode[]>(() => this._summary()?.nodes ?? []);

  readonly hasData = computed(() => this._summary() !== null);
  readonly queueTotalFailed = computed(() => {
    const q = this.queueSummary();
    return q.deliveryFailed + q.validationFailed;
  });

  // ── Mutations ──
  setSummary(summary: DashboardSummary): void {
    this._summary.set(summary);
    this._lastRefreshedAt.set(new Date());
    this._error.set(null);
  }

  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setError(error: string | null): void {
    this._error.set(error);
  }
}
