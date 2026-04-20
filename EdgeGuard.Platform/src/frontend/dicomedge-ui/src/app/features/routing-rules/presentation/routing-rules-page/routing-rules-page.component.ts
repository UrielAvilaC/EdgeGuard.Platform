import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faPlus,
  faSync,
  faPen,
  faToggleOn,
  faToggleOff,
  faTrash,
  faFilter,
  faXmark,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { NodesApiService } from '../../../nodes/infrastructure/nodes-api.service';
import {
  RoutingRule,
  CreateRoutingRuleRequest,
  UpdateRoutingRuleRequest,
} from '../../models/routing-rule.models';
import { RoutingRulesStore } from '../../services/routing-rules.store';
import { RoutingRulesFacade } from '../../services/routing-rules.facade';
import {
  RoutingRuleFormDialog,
  RoutingRuleFormDialogData,
} from '../routing-rule-form-dialog/routing-rule-form-dialog.component';

@Component({
  selector: 'app-routing-rules-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [RoutingRulesStore, RoutingRulesFacade],
  imports: [
    FormsModule,
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiIconButton,
    UiDataTable,
    UiCellDef,
    UiChip,
    UiAlert,
    UiSearchBar,
    UiSlideToggle,
    RelativeTimePipe,
  ],
  templateUrl: './routing-rules-page.component.html',
  styleUrl: './routing-rules-page.component.scss',
})
export default class RoutingRulesPage implements OnInit {
  protected readonly facade = inject(RoutingRulesFacade);
  private readonly dialog = inject(MatDialog);
  private readonly nodesApi = inject(NodesApiService);

  protected readonly faPlus = faPlus;
  protected readonly faSync = faSync;
  protected readonly faPen = faPen;
  protected readonly faToggleOn = faToggleOn;
  protected readonly faToggleOff = faToggleOff;
  protected readonly faTrash = faTrash;
  protected readonly faFilter = faFilter;
  protected readonly faXmark = faXmark;

  protected filtersExpanded = false;
  protected nodeOptions: { id: string; name: string }[] = [];

  protected readonly columns: TableColumn<RoutingRule>[] = [
    { key: 'priority', header: 'Prioridad', sortable: true, width: '8%', align: 'center' },
    { key: 'name', header: 'Nombre', sortable: true, width: '18%' },
    { key: 'matchMessageType', header: 'Tipo Msg', width: '10%' },
    { key: 'matchTriggerEvent', header: 'Evento', width: '10%' },
    { key: 'matchSendingFacility', header: 'Facility', width: '12%' },
    { key: 'targetNodeId', header: 'Nodo Destino', width: '14%' },
    { key: 'matchCount', header: 'Coincidencias', sortable: true, width: '10%', align: 'center' },
    { key: 'lastMatchedAt', header: 'Última coincidencia', sortable: true, width: '12%' },
    { key: 'actions', header: '', width: '10%' },
  ];

  ngOnInit(): void {
    this.facade.loadRules();
    this.nodesApi.getActive().subscribe(nodes => {
      this.nodeOptions = nodes.map(n => ({ id: n.id, name: n.name }));
    });
  }

  protected onSearchChange(search: string): void {
    this.facade.updateFilter({ search: search || undefined });
  }

  protected onEnabledChange(isEnabled: boolean): void {
    this.facade.updateFilter({ isEnabled: isEnabled || undefined });
  }

  protected getNodeName(nodeId: string): string {
    return this.nodeOptions.find(n => n.id === nodeId)?.name ?? nodeId;
  }

  protected openCreateDialog(): void {
    this.dialog.open(RoutingRuleFormDialog, {
      data: { nodeOptions: this.nodeOptions } satisfies RoutingRuleFormDialogData,
    }).afterClosed().subscribe((result: CreateRoutingRuleRequest | null) => {
      if (result) {
        this.facade.createRule(result);
      }
    });
  }

  protected openEditDialog(rule: RoutingRule): void {
    this.dialog.open(RoutingRuleFormDialog, {
      data: { rule, nodeOptions: this.nodeOptions } satisfies RoutingRuleFormDialogData,
    }).afterClosed().subscribe((result: UpdateRoutingRuleRequest | null) => {
      if (result) {
        this.facade.updateRule(rule.id, result);
      }
    });
  }

  protected confirmDelete(rule: RoutingRule): void {
    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Eliminar regla de ruteo',
        message: `¿Eliminar la regla "${rule.name}"? Esta acción no se puede deshacer.`,
        confirmText: 'Eliminar',
        confirmColor: 'warn',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.facade.deleteRule(rule.id);
      }
    });
  }
}
