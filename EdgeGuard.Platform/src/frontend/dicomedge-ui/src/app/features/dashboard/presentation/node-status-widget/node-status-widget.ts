import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state';
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
  template: `
    <mat-card class="widget-card">
      <div class="widget-header">
        <div class="widget-title-row">
          <fa-icon icon="server" class="widget-icon text-violet-600"></fa-icon>
          <h3 class="widget-title">Estado de Nodos</h3>
        </div>
        <a routerLink="/nodes" class="widget-link">Ver todos</a>
      </div>

      @if (facade.nodes().length === 0) {
        <ui-empty-state title="Sin nodos configurados" />
      } @else {
        <div class="nodes-list">
          @for (node of facade.nodes(); track node.id) {
            <div class="node-row">
              <div class="node-info">
                <div class="node-main">
                  <span class="node-name">{{ node.name }}</span>
                  <ui-status-badge [status]="node.status" />
                </div>
                <div class="node-meta">
                  <span>{{ node.aeTitle }}</span>
                  <span class="meta-sep">·</span>
                  <span>{{ node.location ?? node.ipAddress }}</span>
                  @if (node.lastHeartbeatAt) {
                    <span class="meta-sep">·</span>
                    <span>{{ node.lastHeartbeatAt | relativeTime }}</span>
                  }
                </div>
              </div>

              <div class="node-storage">
                <div class="storage-bar-container">
                  <div
                    class="storage-bar-fill"
                    [style.width.%]="storagePercent(node)"
                    [class.storage-warning]="storagePercent(node) > 80"
                    [class.storage-danger]="storagePercent(node) > 95"
                  ></div>
                </div>
                <span class="storage-label">{{ storagePercent(node) }}%</span>
              </div>
            </div>
          }
        </div>
      }
    </mat-card>
  `,
  styles: `
    .widget-card {
      padding: 0 !important;
      overflow: hidden;
    }

    .widget-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px 20px 12px;
      border-bottom: 1px solid var(--eg-border-subtle);
    }

    .widget-title-row {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .widget-icon {
      font-size: 1rem;
      opacity: 0.8;
    }

    .widget-title {
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--eg-text-primary);
      margin: 0;
    }

    .widget-link {
      font-size: 0.8125rem;
      font-weight: 500;
      color: #0078d4;
      text-decoration: none;
      transition: opacity var(--eg-transition-fast);

      &:hover { opacity: 0.8; }
    }

    .nodes-list {
      display: flex;
      flex-direction: column;
    }

    .node-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 12px 20px;
      border-bottom: 1px solid var(--eg-border-subtle);
      transition: background var(--eg-transition-fast);

      &:last-child { border-bottom: none; }
      &:hover { background: var(--eg-surface-container); }
    }

    .node-info {
      min-width: 0;
      flex: 1;
    }

    .node-main {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 2px;
    }

    .node-name {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--eg-text-primary);
    }

    .node-meta {
      font-size: 0.75rem;
      color: var(--eg-text-muted);
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .meta-sep {
      opacity: 0.5;
    }

    .node-storage {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-shrink: 0;
      min-width: 120px;
    }

    .storage-bar-container {
      flex: 1;
      height: 6px;
      background: var(--eg-surface-container-high);
      border-radius: 3px;
      overflow: hidden;
    }

    .storage-bar-fill {
      height: 100%;
      border-radius: 3px;
      background: #0078d4;
      transition: width var(--eg-transition-normal);
    }

    .storage-bar-fill.storage-warning {
      background: #d97706;
    }

    .storage-bar-fill.storage-danger {
      background: #dc2626;
    }

    .storage-label {
      font-size: 0.6875rem;
      font-weight: 600;
      color: var(--eg-text-muted);
      min-width: 32px;
      text-align: right;
      font-variant-numeric: tabular-nums;
    }
  `,
})
export class NodeStatusWidget {
  protected readonly facade = inject(DashboardFacade);

  protected storagePercent(node: DashboardNode): number {
    if (node.maxStorageMb <= 0) return 0;
    const used = node.maxStorageMb - node.availableStorageMb;
    return Math.round((used / node.maxStorageMb) * 100);
  }
}
