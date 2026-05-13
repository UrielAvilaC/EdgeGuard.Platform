import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faCircle,
  faPlug,
  faNetworkWired,
} from '@fortawesome/free-solid-svg-icons';

import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { Hl7ListenerStatus } from '../../models/hl7.models';

@Component({
  selector: 'app-hl7-listener-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule, UiChip],
  templateUrl: './hl7-listener-status.component.html',
  styleUrl: './hl7-listener-status.component.scss'
})
export class Hl7ListenerStatusCard {
  readonly status = input.required<Hl7ListenerStatus | null>();

  protected readonly faCircle = faCircle;
  protected readonly faPlug = faPlug;
  protected readonly faNetworkWired = faNetworkWired;
}
