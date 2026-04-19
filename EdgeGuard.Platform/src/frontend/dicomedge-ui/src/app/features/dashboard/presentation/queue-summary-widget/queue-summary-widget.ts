import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { DashboardFacade } from '../../services/dashboard.facade';

interface QueueStage {
  label: string;
  value: number;
  color: string;
}

@Component({
  selector: 'app-queue-summary-widget',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, RouterLink, FontAwesomeModule],
  template: `
    <mat-card class="widget-card">
      <div class="widget-header">
        <div class="widget-title-row">
          <fa-icon icon="sync" class="widget-icon text-cyan-600"></fa-icon>
          <h3 class="widget-title">Pipeline de Mensajes</h3>
        </div>
        <a routerLink="/queue" class="widget-link">Ver cola</a>
      </div>

      <div class="queue-content">
        <div class="pipeline-total">
          <span class="pipeline-value">{{ facade.queueSummary().totalInPipeline }}</span>
          <span class="pipeline-label">en pipeline</span>
        </div>

        <div class="queue-bar">
          @for (stage of stages(); track stage.label) {
            @if (stage.value > 0) {
              <div
                class="queue-bar-segment"
                [style.flex]="stage.value"
                [style.background]="stage.color"
                [title]="stage.label + ': ' + stage.value"
              ></div>
            }
          }
        </div>

        <div class="queue-stages">
          @for (stage of stages(); track stage.label) {
            <div class="stage-item">
              <div class="stage-dot" [style.background]="stage.color"></div>
              <span class="stage-label">{{ stage.label }}</span>
              <span class="stage-value">{{ stage.value }}</span>
            </div>
          }
        </div>

        @if (hl7Status(); as hl7) {
          <div class="hl7-status">
            <div class="hl7-indicator" [class.hl7-running]="hl7.isRunning" [class.hl7-stopped]="!hl7.isRunning"></div>
            <span class="hl7-text">
              HL7 Listener {{ hl7.isRunning ? 'activo' : 'detenido' }}
              @if (hl7.isRunning) {
                — Puerto {{ hl7.port }}, {{ hl7.activeConnections }} conexión{{ hl7.activeConnections !== 1 ? 'es' : '' }}
              }
            </span>
          </div>
        }
      </div>
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

    .queue-content {
      padding: 20px;
    }

    .pipeline-total {
      display: flex;
      align-items: baseline;
      gap: 8px;
      margin-bottom: 16px;
    }

    .pipeline-value {
      font-size: 2rem;
      font-weight: 700;
      color: var(--eg-text-primary);
      letter-spacing: -0.02em;
      line-height: 1;
      font-variant-numeric: tabular-nums;
    }

    .pipeline-label {
      font-size: 0.875rem;
      color: var(--eg-text-muted);
    }

    .queue-bar {
      display: flex;
      height: 8px;
      border-radius: 4px;
      overflow: hidden;
      gap: 2px;
      margin-bottom: 16px;
      background: var(--eg-surface-container);
    }

    .queue-bar-segment {
      border-radius: 4px;
      transition: flex var(--eg-transition-normal);
      min-width: 4px;
    }

    .queue-stages {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
      gap: 8px 16px;
    }

    .stage-item {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 0.8125rem;
    }

    .stage-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .stage-label {
      color: var(--eg-text-secondary);
      flex: 1;
    }

    .stage-value {
      font-weight: 600;
      color: var(--eg-text-primary);
      font-variant-numeric: tabular-nums;
    }

    .hl7-status {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 16px;
      padding-top: 16px;
      border-top: 1px solid var(--eg-border-subtle);
    }

    .hl7-indicator {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .hl7-running {
      background: #22c55e;
      box-shadow: 0 0 6px rgba(34, 197, 94, 0.4);
    }

    .hl7-stopped {
      background: #ef4444;
    }

    .hl7-text {
      font-size: 0.8125rem;
      color: var(--eg-text-secondary);
    }
  `,
})
export class QueueSummaryWidget {
  protected readonly facade = inject(DashboardFacade);
  protected readonly hl7Status = this.facade.hl7Status;

  protected readonly stages = computed<QueueStage[]>(() => {
    const q = this.facade.queueSummary();
    return [
      { label: 'Pendiente valid.', value: q.pendingValidation, color: '#94a3b8' },
      { label: 'Validado', value: q.validated, color: '#0ea5e9' },
      { label: 'Enrutado', value: q.routed, color: '#8b5cf6' },
      { label: 'En cola', value: q.queued, color: '#f59e0b' },
      { label: 'Despachando', value: q.dispatching, color: '#0078d4' },
      { label: 'Entregado', value: q.delivered, color: '#22c55e' },
      { label: 'Fallo entrega', value: q.deliveryFailed, color: '#ef4444' },
      { label: 'Fallo valid.', value: q.validationFailed, color: '#f97316' },
    ];
  });
}
