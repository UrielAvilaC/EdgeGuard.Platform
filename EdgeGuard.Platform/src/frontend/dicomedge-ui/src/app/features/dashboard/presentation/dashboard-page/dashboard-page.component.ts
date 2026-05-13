import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { DashboardFacade } from '../../services/dashboard.facade';
import { DashboardStore } from '../../services/dashboard.store';
import { StatsOverview } from '../stats-overview/stats-overview.component';
import { RecentStudiesWidget } from '../recent-studies-widget/recent-studies-widget.component';
import { NodeStatusWidget } from '../node-status-widget/node-status-widget.component';
import { QueueSummaryWidget } from '../queue-summary-widget/queue-summary-widget.component';

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
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export default class DashboardPage {
  protected readonly facade = inject(DashboardFacade);

  constructor() {
    this.facade.startAutoRefresh();
  }
}
