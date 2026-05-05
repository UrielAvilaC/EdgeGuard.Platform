import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  DestroyRef,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft,
  faSync,
  faChartBar,
  faNetworkWired,
  faDatabase,
  faCheckCircle,
} from '@fortawesome/free-solid-svg-icons';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize as rxFinalize } from 'rxjs';

import { DecimalPipe } from '@angular/common';
import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { NodesApiService } from '../../infrastructure/nodes-api.service';
import { NodeTelemetry } from '../../models/node.models';

@Component({
  selector: 'app-node-telemetry-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiLoadingSpinner,
    UiAlert,
    UiEmptyState,
    RelativeTimePipe,
    FileSizePipe,
    DecimalPipe,
  ],
  templateUrl: './node-telemetry-page.component.html',
})
export default class NodeTelemetryPage {
  private readonly api       = inject(NodesApiService);
  private readonly route     = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly faArrowLeft    = faArrowLeft;
  protected readonly faSync         = faSync;
  protected readonly faChartBar     = faChartBar;
  protected readonly faNetworkWired = faNetworkWired;
  protected readonly faDatabase     = faDatabase;
  protected readonly faCheckCircle  = faCheckCircle;

  protected readonly nodeId   = signal<string>('');
  protected readonly records  = signal<NodeTelemetry[]>([]);
  protected readonly loading  = signal(false);
  protected readonly error    = signal<string | null>(null);

  // ── Computed summary from latest record ──
  protected readonly latest = computed(() => this.records()[0] ?? null);

  protected readonly acceptanceRate = computed(() => {
    const r = this.latest();
    if (!r || r.totalAssociations === 0) return null;
    return ((r.acceptedAssociations / r.totalAssociations) * 100).toFixed(1);
  });

  constructor() {
    this.nodeId.set(this.route.snapshot.paramMap.get('id') ?? '');
    this.loadTelemetry();
  }

  protected loadTelemetry(): void {
    const id = this.nodeId();
    if (!id) return;

    this.loading.set(true);
    this.error.set(null);

    this.api.getTelemetry(id).pipe(
      rxFinalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next:  (data) => this.records.set(data),
      error: ()     => this.error.set('No se pudo cargar la telemetría del nodo'),
    });
  }

  protected formatDuration(ms: number | null): string {
    if (ms === null) return '—';
    if (ms < 1000) return `${ms.toFixed(0)} ms`;
    return `${(ms / 1000).toFixed(1)} s`;
  }

  protected formatPeriod(start: string, end: string): string {
    const s = new Date(start);
    const e = new Date(end);
    const diffMin = Math.round((e.getTime() - s.getTime()) / 60_000);
    return diffMin < 60 ? `${diffMin} min` : `${(diffMin / 60).toFixed(1)} h`;
  }
}
