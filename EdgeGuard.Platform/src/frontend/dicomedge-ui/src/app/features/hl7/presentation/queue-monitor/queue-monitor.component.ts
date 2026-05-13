import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faArrowRight } from '@fortawesome/free-solid-svg-icons';

import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';

import {
  QueueSummary,
  Hl7MessageQueued,
  Hl7DispatchStatus,
  DISPATCH_STATUS_OPTIONS,
} from '../../models/hl7.models';

@Component({
  selector: 'app-queue-monitor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule, DecimalPipe, RelativeTimePipe],
  templateUrl: './queue-monitor.component.html',
  styleUrl: './queue-monitor.component.scss'
})
export class QueueMonitor {
  readonly summary = input.required<QueueSummary | null>();
  readonly queuedMessages = input.required<Hl7MessageQueued[]>();
  readonly selectedStatus = input<Hl7DispatchStatus | null>(null);
  readonly selectStatus = output<Hl7DispatchStatus>();

  protected readonly faArrowRight = faArrowRight;

  protected readonly stages: { key: Hl7DispatchStatus; label: string; colorClass: string }[] = [
    { key: 'PendingValidation', label: 'Pendiente validación', colorClass: 'text-gray-600 dark:text-gray-300' },
    { key: 'Validated', label: 'Validados', colorClass: 'text-blue-600 dark:text-blue-400' },
    { key: 'Routed', label: 'Enrutados', colorClass: 'text-indigo-600 dark:text-indigo-400' },
    { key: 'Queued', label: 'En cola', colorClass: 'text-amber-600 dark:text-amber-400' },
    { key: 'Dispatching', label: 'Despachando', colorClass: 'text-orange-600 dark:text-orange-400' },
    { key: 'Delivered', label: 'Entregados', colorClass: 'text-emerald-600 dark:text-emerald-400' },
    { key: 'ValidationFailed', label: 'Validación fallida', colorClass: 'text-red-600 dark:text-red-400' },
    { key: 'DeliveryFailed', label: 'Entrega fallida', colorClass: 'text-red-600 dark:text-red-400' },
  ];

  protected getStageValue(summary: QueueSummary, key: Hl7DispatchStatus): number {
    const map: Record<Hl7DispatchStatus, keyof QueueSummary> = {
      PendingValidation: 'pendingValidation',
      Validated: 'validated',
      ValidationFailed: 'validationFailed',
      Routed: 'routed',
      Queued: 'queued',
      Dispatching: 'dispatching',
      Delivered: 'delivered',
      DeliveryFailed: 'deliveryFailed',
    };
    return (summary[map[key]] as number) ?? 0;
  }
}
