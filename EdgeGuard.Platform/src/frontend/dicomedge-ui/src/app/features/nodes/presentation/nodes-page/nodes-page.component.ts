import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faPlus,
  faSync,
  faHardDrive,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { Node, CreateNodeRequest } from '../../models/node.models';
import { NodesStore } from '../../services/nodes.store';
import { NodesFacade } from '../../services/nodes.facade';
import { NodeFilters } from '../node-filters/node-filters.component';
import { NodeFormDialog, NodeFormDialogData } from '../node-form-dialog/node-form-dialog.component';

@Component({
  selector: 'app-nodes-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [NodesStore, NodesFacade],
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiDataTable,
    UiCellDef,
    UiStatusBadge,
    UiChip,
    UiAlert,
    RelativeTimePipe,
    NodeFilters,
  ],
  templateUrl: './nodes-page.component.html',
  styleUrl: './nodes-page.component.scss'
})
export default class NodesPage {
  protected readonly facade = inject(NodesFacade);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  protected readonly faPlus = faPlus;
  protected readonly faSync = faSync;
  protected readonly faHardDrive = faHardDrive;

  protected readonly columns: TableColumn<Node>[] = [
    { key: 'name', header: 'Nombre', sortable: true, width: '18%' },
    { key: 'aeTitle', header: 'AE Title', sortable: true, width: '12%' },
    { key: 'ipAddress', header: 'IP:Puerto', sortable: true, width: '14%' },
    { key: 'status', header: 'Estado', sortable: true, width: '12%' },
    { key: 'lastHeartbeatAt', header: 'Último Heartbeat', sortable: true, width: '14%' },
    { key: 'availableStorageMb', header: 'Storage', width: '16%' },
    { key: 'errorsLast24Hours', header: 'Errores 24h', sortable: true, width: '10%', align: 'center' },
  ];

  constructor() {
    this.facade.loadNodes();
  }

  protected onRowClick(node: Node): void {
    this.router.navigate(['/nodes', node.id]);
  }

  protected openCreateDialog(): void {
    this.dialog.open(NodeFormDialog, {
      data: {} satisfies NodeFormDialogData,
    }).afterClosed().subscribe((result: CreateNodeRequest | null) => {
      if (result) {
        this.facade.createNode(result);
      }
    });
  }

  protected storagePercent(node: Node): number {
    if (node.maxStorageMb === 0) return 0;
    return Math.round(((node.maxStorageMb - node.availableStorageMb) / node.maxStorageMb) * 100);
  }
}
