import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header';
import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { DashboardFacade } from '../../services/dashboard.facade';
import { DashboardStore } from '../../services/dashboard.store';
import { StatsOverview } from '../stats-overview/stats-overview';
import { RecentStudiesWidget } from '../recent-studies-widget/recent-studies-widget';
import { NodeStatusWidget } from '../node-status-widget/node-status-widget';
import { QueueSummaryWidget } from '../queue-summary-widget/queue-summary-widget';

@Component({
  selector: 'app-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DashboardStore, DashboardFacade],
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiAlert,
    UiLoadingSpinner,
    RelativeTimePipe,
    StatsOverview,
    RecentStudiesWidget,
    NodeStatusWidget,
    QueueSummaryWidget,
  ],
  template: `
    <div class="dashboard">
      <ui-page-header
        title="Dashboard"
        subtitle="Vista general"
        description="Resumen del estado de la plataforma DICOM en tiempo real"
      >
        @if (facade.lastRefreshedAt(); as ts) {
          <span class="refresh-ts">
            <fa-icon icon="clock" class="mr-1"></fa-icon>
            {{ ts | relativeTime }}
          </span>
        }
        <span class="inline-flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-full"
              [class]="facade.signalRConnected()
                ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
                : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400'"
              [attr.aria-label]="facade.signalRConnected() ? 'Tiempo real activo' : 'Tiempo real desconectado'">
          <span class="w-2 h-2 rounded-full"
                [class]="facade.signalRConnected() ? 'bg-emerald-500 animate-pulse' : 'bg-gray-400'">
          </span>
          {{ facade.signalRConnected() ? 'En vivo' : 'Desconectado' }}
        </span>
        <ui-button
          variant="secondary"
          size="sm"
          icon="sync"
          [loading]="facade.loading()"
          (clicked)="facade.refresh()"
        >
          Actualizar
        </ui-button>
      </ui-page-header>

      @if (facade.error(); as err) {
        <ui-alert type="error" [dismissible]="true" class="mb-4 block">
          {{ err }}
        </ui-alert>
      }

      @if (!facade.hasData() && facade.loading()) {
        <ui-loading-spinner />
      } @else {
        <div class="dashboard-grid">
          <!-- KPIs row -->
          <section class="grid-full">
            <app-stats-overview />
          </section>

          <!-- Main content row -->
          <section class="grid-main">
            <app-recent-studies-widget />
          </section>

          <section class="grid-side">
            <app-node-status-widget />
          </section>

          <!-- Full width queue -->
          <section class="grid-full">
            <app-queue-summary-widget />
          </section>
        </div>
      }
    </div>
  `,
  styles: `
    .dashboard-grid {
      display: grid;
      grid-template-columns: 1fr;
      gap: 20px;

      @media (min-width: 1024px) {
        grid-template-columns: 1fr 1fr;
      }

      @media (min-width: 1280px) {
        grid-template-columns: 3fr 2fr;
      }
    }

    .grid-full {
      grid-column: 1 / -1;
    }

    .grid-main {
      grid-column: 1 / -1;

      @media (min-width: 1024px) {
        grid-column: 1 / 2;
      }
    }

    .grid-side {
      grid-column: 1 / -1;

      @media (min-width: 1024px) {
        grid-column: 2 / 3;
      }
    }

    .refresh-ts {
      font-size: 0.75rem;
      color: var(--eg-text-muted);
      display: flex;
      align-items: center;
      white-space: nowrap;
    }
  `,
})
export default class DashboardPage {
  protected readonly facade = inject(DashboardFacade);

  constructor() {
    this.facade.startAutoRefresh();
  }
}
