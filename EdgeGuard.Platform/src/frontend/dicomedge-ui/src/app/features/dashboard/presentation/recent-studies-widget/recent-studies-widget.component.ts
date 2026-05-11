import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { DashboardFacade } from '../../services/dashboard.facade';

@Component({
  selector: 'app-recent-studies-widget',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatCardModule,
    RouterLink,
    FontAwesomeModule,
    UiStatusBadge,
    RelativeTimePipe,
    FileSizePipe,
    UiEmptyState,
  ],
  templateUrl: './recent-studies-widget.component.html',
  styleUrl: './recent-studies-widget.component.scss'
})
export class RecentStudiesWidget {
  protected readonly facade = inject(DashboardFacade);
}
