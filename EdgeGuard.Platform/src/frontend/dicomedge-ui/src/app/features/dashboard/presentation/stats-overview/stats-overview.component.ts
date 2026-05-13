import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { UiStatCard } from '../../../../shared/components/ui-stat-card/ui-stat-card.component';
import { DashboardFacade } from '../../services/dashboard.facade';

@Component({
  selector: 'app-stats-overview',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [UiStatCard],
  templateUrl: './stats-overview.component.html',
  styleUrl: './stats-overview.component.scss'
})
export class StatsOverview {
  protected readonly facade = inject(DashboardFacade);
}
