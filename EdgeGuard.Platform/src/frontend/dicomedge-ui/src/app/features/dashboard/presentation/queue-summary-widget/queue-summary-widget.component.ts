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
  templateUrl: './queue-summary-widget.component.html',
  styleUrl: './queue-summary-widget.component.scss'
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
