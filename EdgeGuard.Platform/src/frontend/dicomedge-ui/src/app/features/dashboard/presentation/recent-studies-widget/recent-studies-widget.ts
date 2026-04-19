import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state';
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
  template: `
    <mat-card class="widget-card">
      <div class="widget-header">
        <div class="widget-title-row">
          <fa-icon icon="clipboard-list" class="widget-icon text-blue-600"></fa-icon>
          <h3 class="widget-title">Estudios Recientes</h3>
        </div>
        <a routerLink="/studies" class="widget-link">Ver todos</a>
      </div>

      @if (facade.recentStudies().length === 0) {
        <ui-empty-state title="Sin estudios recientes" />
      } @else {
        <div class="studies-list">
          @for (study of facade.recentStudies(); track study.id) {
            <div class="study-row">
              <div class="study-info">
                <div class="study-main">
                  <span class="study-patient">{{ study.patientName ?? 'Desconocido' }}</span>
                  @if (study.isUrgent) {
                    <span class="urgent-badge">URGENTE</span>
                  }
                </div>
                <div class="study-meta">
                  <span>{{ study.studyDescription ?? 'Sin descripción' }}</span>
                  <span class="meta-separator">·</span>
                  <span>{{ study.totalSizeBytes | fileSize }}</span>
                  <span class="meta-separator">·</span>
                  <span>{{ study.createdAt | relativeTime }}</span>
                </div>
              </div>
              <div class="study-actions">
                <span class="study-instances">{{ study.seriesCount }}S / {{ study.instanceCount }}I</span>
                <ui-status-badge [status]="study.status" />
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

    .studies-list {
      display: flex;
      flex-direction: column;
    }

    .study-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 12px 20px;
      border-bottom: 1px solid var(--eg-border-subtle);
      transition: background var(--eg-transition-fast);

      &:last-child { border-bottom: none; }
      &:hover { background: var(--eg-surface-container); }
    }

    .study-info {
      min-width: 0;
      flex: 1;
    }

    .study-main {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 2px;
    }

    .study-patient {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--eg-text-primary);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .urgent-badge {
      font-size: 0.625rem;
      font-weight: 700;
      padding: 1px 6px;
      border-radius: 4px;
      background: #dc2626;
      color: #fff;
      letter-spacing: 0.04em;
      flex-shrink: 0;
    }

    .study-meta {
      font-size: 0.75rem;
      color: var(--eg-text-muted);
      display: flex;
      align-items: center;
      gap: 4px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .meta-separator {
      opacity: 0.5;
    }

    .study-actions {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-shrink: 0;
    }

    .study-instances {
      font-size: 0.75rem;
      font-weight: 500;
      color: var(--eg-text-muted);
      font-variant-numeric: tabular-nums;
    }
  `,
})
export class RecentStudiesWidget {
  protected readonly facade = inject(DashboardFacade);
}
