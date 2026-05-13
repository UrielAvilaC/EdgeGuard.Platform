import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faEye } from '@fortawesome/free-solid-svg-icons';

import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { ChipColor } from '../../../../shared/components/ui-chip/ui-chip.component';
import { Hl7MessageSummary, MESSAGE_STATUS_OPTIONS } from '../../models/hl7.models';

@Component({
  selector: 'app-hl7-message-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule, UiIconButton, UiChip, RelativeTimePipe],
  templateUrl: './hl7-message-list.component.html',
  styleUrl: './hl7-message-list.component.scss'
})
export class Hl7MessageList {
  readonly messages = input.required<Hl7MessageSummary[]>();
  readonly viewDetail = output<string>();

  protected readonly faEye = faEye;

  protected getStatusColor(status: string): ChipColor {
    return MESSAGE_STATUS_OPTIONS.find(o => o.value === status)?.color ?? 'default';
  }

  protected getStatusLabel(status: string): string {
    return MESSAGE_STATUS_OPTIONS.find(o => o.value === status)?.label ?? status;
  }
}
