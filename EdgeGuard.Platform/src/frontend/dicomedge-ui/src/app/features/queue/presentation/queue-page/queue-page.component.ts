import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faSync,
  faInbox,
  faCircleCheck,
  faCircleXmark,
  faClock,
  faTruckFast,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiChip, ChipColor } from '../../../../shared/components/ui-chip/ui-chip.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { DispatchStatus, DISPATCH_STATUS_OPTIONS } from '../../models/queue.models';
import { QueueStore } from '../../services/queue.store';
import { QueueFacade } from '../../services/queue.facade';

@Component({
  selector: 'app-queue-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [QueueStore, QueueFacade],
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiAlert,
    UiChip,
    RelativeTimePipe,
  ],
  templateUrl: './queue-page.component.html',
  styleUrl: './queue-page.component.scss',
})
export default class QueuePage {
  protected readonly facade = inject(QueueFacade);

  protected readonly faSync = faSync;
  protected readonly faInbox = faInbox;
  protected readonly faCircleCheck = faCircleCheck;
  protected readonly faCircleXmark = faCircleXmark;
  protected readonly faClock = faClock;
  protected readonly faTruckFast = faTruckFast;

  protected readonly statusOptions = DISPATCH_STATUS_OPTIONS;

  constructor() {
    this.facade.loadSummary();
  }

  protected selectStatus(status: DispatchStatus): void {
    if (this.facade.selectedStatus() === status) {
      this.facade.clearStatusFilter();
    } else {
      this.facade.loadByStatus(status);
    }
  }

  protected getStatusLabel(status: DispatchStatus): string {
    return DISPATCH_STATUS_OPTIONS.find(s => s.value === status)?.label ?? status;
  }

  protected getStatusColor(status: DispatchStatus): ChipColor {
    return DISPATCH_STATUS_OPTIONS.find(s => s.value === status)?.color ?? 'default';
  }

  protected getSummaryValue(status: DispatchStatus): number {
    const s = this.facade.summary();
    if (!s) return 0;
    const map: Record<DispatchStatus, number> = {
      PendingValidation: s.pendingValidation,
      Validated: s.validated,
      Routed: s.routed,
      Queued: s.queued,
      Dispatching: s.dispatching,
      Delivered: s.delivered,
      DeliveryFailed: s.deliveryFailed,
      ValidationFailed: s.validationFailed,
    };
    return map[status];
  }
}
