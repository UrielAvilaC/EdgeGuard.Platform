import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { UiStatCard } from '../../../../shared/components/ui-stat-card/ui-stat-card';
import { DashboardFacade } from '../../services/dashboard.facade';

@Component({
  selector: 'app-stats-overview',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [UiStatCard],
  template: `
    <div class="stats-grid eg-stagger">
      <ui-stat-card
        icon="clipboard-list"
        label="Total Estudios"
        [value]="facade.totalStudies()"
        iconBgClass="bg-blue-600"
      />
      <ui-stat-card
        icon="user"
        label="Pacientes"
        [value]="facade.totalPatients()"
        iconBgClass="bg-emerald-600"
      />
      <ui-stat-card
        icon="server"
        label="Nodos Activos"
        [value]="facade.activeNodes() + ' / ' + facade.totalNodes()"
        iconBgClass="bg-violet-600"
      />
      <ui-stat-card
        icon="clock"
        label="Pendientes PACS"
        [value]="facade.pendingPacsStudies()"
        iconBgClass="bg-amber-500"
        [trendDirection]="facade.pendingPacsStudies() > 10 ? 'up' : 'neutral'"
      />
      <ui-stat-card
        icon="exclamation-triangle"
        label="Fallidos"
        [value]="facade.failedStudies()"
        iconBgClass="bg-red-500"
        [trendDirection]="facade.failedStudies() > 0 ? 'down' : 'neutral'"
      />
      <ui-stat-card
        icon="sync"
        label="En Cola"
        [value]="facade.queueSummary().totalInPipeline"
        iconBgClass="bg-cyan-600"
      />
    </div>
  `,
  styles: `
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 16px;
    }
  `,
})
export class StatsOverview {
  protected readonly facade = inject(DashboardFacade);
}
