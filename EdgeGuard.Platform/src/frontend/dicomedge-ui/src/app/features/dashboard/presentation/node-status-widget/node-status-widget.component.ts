import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { DashboardFacade } from '../../services/dashboard.facade';
import { DashboardNode } from '../../models/dashboard.models';
import {
  StorageReading,
  formatMb,
  readStorage,
  storageBarClass,
} from '../../../../shared/utils/storage-usage';

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

  protected storage(node: DashboardNode): StorageReading {
    return readStorage(node);
  }

  protected readonly barClass = storageBarClass;
  protected readonly format = formatMb;
}
