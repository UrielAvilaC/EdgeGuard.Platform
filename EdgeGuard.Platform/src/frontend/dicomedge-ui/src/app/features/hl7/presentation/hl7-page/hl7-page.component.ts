import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faSync,
  faNetworkWired,
  faRoute,
  faLayerGroup,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import {
  CreateRoutingRuleRequest,
  Hl7DispatchStatus,
  RoutingRule,
  UpdateRoutingRuleRequest,
} from '../../models/hl7.models';
import { Node } from '../../../nodes/models/node.models';
import { NodesApiService } from '../../../nodes/infrastructure/nodes-api.service';
import { Hl7Store } from '../../services/hl7.store';
import { Hl7Facade } from '../../services/hl7.facade';
import { Hl7ListenerStatusCard } from '../hl7-listener-status/hl7-listener-status.component';
import { Hl7MessageList } from '../hl7-message-list/hl7-message-list.component';
import { Hl7MessageDetailDialog, Hl7MessageDetailDialogData } from '../hl7-message-detail-dialog/hl7-message-detail-dialog.component';
import { RoutingRulesList } from '../routing-rules-list/routing-rules-list.component';
import { RoutingRuleFormDialog, RoutingRuleFormDialogData } from '../routing-rule-form-dialog/routing-rule-form-dialog.component';
import { QueueMonitor } from '../queue-monitor/queue-monitor.component';

@Component({
  selector: 'app-hl7-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [Hl7Store, Hl7Facade],
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiAlert,
    Hl7ListenerStatusCard,
    Hl7MessageList,
    RoutingRulesList,
    QueueMonitor,
  ],
  templateUrl: './hl7-page.component.html',
  styleUrl: './hl7-page.component.scss'
})
export default class Hl7Page {
  protected readonly facade = inject(Hl7Facade);
  private readonly dialog = inject(MatDialog);
  private readonly nodesApi = inject(NodesApiService);

  protected readonly faSync = faSync;
  protected readonly faNetworkWired = faNetworkWired;
  protected readonly faRoute = faRoute;
  protected readonly faLayerGroup = faLayerGroup;

  private allNodes: Node[] = [];

  protected readonly tabs = [
    { key: 'listener' as const, label: 'Listener', icon: faNetworkWired },
    { key: 'routing' as const, label: 'Reglas de enrutamiento', icon: faRoute },
    { key: 'queue' as const, label: 'Cola de mensajes', icon: faLayerGroup },
  ];

  constructor() {
    this.facade.loadListenerStatus();
    this.facade.loadRecentMessages();
    this.nodesApi.getActive().subscribe(nodes => (this.allNodes = nodes));
  }

  protected onTabChange(tab: 'listener' | 'routing' | 'queue'): void {
    this.facade.setActiveTab(tab);
    this.refreshCurrentTab();
  }

  protected refreshCurrentTab(): void {
    switch (this.facade.activeTab()) {
      case 'listener':
        this.facade.loadListenerStatus();
        this.facade.loadRecentMessages();
        break;
      case 'routing':
        this.facade.loadRules();
        break;
      case 'queue':
        this.facade.loadQueueSummary();
        this.facade.loadQueuedMessages();
        break;
    }
  }

  protected openMessageDetail(id: string): void {
    this.facade.loadMessageDetail(id);
    // Wait for data then open dialog
    const sub = setInterval(() => {
      const msg = this.facade.selectedMessage();
      if (msg && msg.id === id) {
        clearInterval(sub);
        this.dialog.open(Hl7MessageDetailDialog, {
          data: { message: msg } satisfies Hl7MessageDetailDialogData,
        }).afterClosed().subscribe((result?: { action: string; id: string }) => {
          if (result?.action === 'reprocess') {
            this.facade.reprocessMessage(result.id);
          }
        });
      }
    }, 100);
    // Safety timeout
    setTimeout(() => clearInterval(sub), 5000);
  }

  protected onSelectDispatchStatus(status: Hl7DispatchStatus): void {
    this.facade.loadMessagesByDispatchStatus(status);
  }

  protected openCreateRuleDialog(): void {
    this.dialog.open(RoutingRuleFormDialog, {
      data: { nodes: this.allNodes } satisfies RoutingRuleFormDialogData,
      disableClose: true,
    }).afterClosed().subscribe((result: CreateRoutingRuleRequest | null) => {
      if (result) this.facade.createRule(result);
    });
  }

  protected openEditRuleDialog(rule: RoutingRule): void {
    this.dialog.open(RoutingRuleFormDialog, {
      data: { rule, nodes: this.allNodes } satisfies RoutingRuleFormDialogData,
      disableClose: true,
    }).afterClosed().subscribe((result: UpdateRoutingRuleRequest | null) => {
      if (result) this.facade.updateRule(rule.id, result);
    });
  }

  protected confirmDeleteRule(rule: RoutingRule): void {
    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Eliminar regla de enrutamiento',
        message: `¿Eliminar la regla "${rule.name}"? Esta acción no se puede deshacer.`,
        confirmText: 'Eliminar',
        confirmColor: 'warn',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) this.facade.deleteRule(rule.id);
    });
  }
}
