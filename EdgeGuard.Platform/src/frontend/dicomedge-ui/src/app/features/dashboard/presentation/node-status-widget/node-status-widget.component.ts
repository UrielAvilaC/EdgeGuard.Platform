import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { DashboardFacade } from '../../services/dashboard.facade';
import { DashboardNode } from '../../models/dashboard.models';

@Component({
  selector: 'app-node-status-widget',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatCardModule,
    RouterLink,
    FontAwesomeModule,
    UiStatusBadge,
    RelativeTimePipe,
    UiEmptyState,
  ],
  templateUrl: './node-status-widget.component.html',
  styleUrl: './node-status-widget.component.scss'
})
export class NodeStatusWidget {
  protected readonly facade = inject(DashboardFacade);

  protected storagePercent(node: DashboardNode): number {
    if (node.maxStorageMb <= 0) return 0;
    const used = node.maxStorageMb - node.availableStorageMb;
    return Math.round((used / node.maxStorageMb) * 100);
  }
}
