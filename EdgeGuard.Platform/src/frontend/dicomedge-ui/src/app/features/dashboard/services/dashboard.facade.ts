import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, interval, merge, startWith, switchMap, throttleTime } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { DashboardApiService } from '../infrastructure/dashboard-api.service';
import { DashboardStore } from './dashboard.store';

const AUTO_REFRESH_MS = 30_000;
const SIGNALR_THROTTLE_MS = 5_000;

@Injectable()
export class DashboardFacade {
  private readonly api = inject(DashboardApiService);
  private readonly store = inject(DashboardStore);
  private readonly toast = inject(ToastService);
  private readonly signalR = inject(SignalRService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly summary = this.store.summary;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly lastRefreshedAt = this.store.lastRefreshedAt;

  readonly totalStudies = this.store.totalStudies;
  readonly totalPatients = this.store.totalPatients;
  readonly totalNodes = this.store.totalNodes;
  readonly activeNodes = this.store.activeNodes;
  readonly pendingPacsStudies = this.store.pendingPacsStudies;
  readonly failedStudies = this.store.failedStudies;

  readonly queueSummary = this.store.queueSummary;
  readonly hl7Status = this.store.hl7Status;
  readonly recentStudies = this.store.recentStudies;
  readonly nodes = this.store.nodes;
  readonly hasData = this.store.hasData;
  readonly queueTotalFailed = this.store.queueTotalFailed;

  readonly signalRConnected = this.signalR.connected;

  // ── Actions ──
  startAutoRefresh(): void {
    this.initSignalR();

    const timer$ = interval(AUTO_REFRESH_MS);

    const signalR$ = merge(
      this.signalR.on('StudyReceived'),
      this.signalR.on('StudyStatusChanged'),
      this.signalR.on('NodeStatusChanged'),
      this.signalR.on('NodeHeartbeat'),
      this.signalR.on('Hl7MessageReceived'),
      this.signalR.on('WhatsAppNotificationSent'),
    ).pipe(throttleTime(SIGNALR_THROTTLE_MS));

    merge(timer$, signalR$).pipe(
      startWith(0),
      switchMap(() => {
        this.store.setLoading(true);
        return this.api.getSummary().pipe(
          finalize(() => this.store.setLoading(false)),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (summary) => this.store.setSummary(summary),
      error: (err) => {
        this.store.setError(err?.message ?? 'Error al cargar dashboard');
        this.toast.error('No se pudo cargar el resumen del dashboard');
      },
    });
  }

  refresh(): void {
    this.store.setLoading(true);
    this.api.getSummary().pipe(
      finalize(() => this.store.setLoading(false)),
    ).subscribe({
      next: (summary) => this.store.setSummary(summary),
      error: (err) => {
        this.store.setError(err?.message ?? 'Error al cargar dashboard');
        this.toast.error('No se pudo actualizar el dashboard');
      },
    });
  }

  private initSignalR(): void {
    this.signalR.start(this.destroyRef).then(() => {
      this.signalR.joinDashboard();
    });
  }
}
