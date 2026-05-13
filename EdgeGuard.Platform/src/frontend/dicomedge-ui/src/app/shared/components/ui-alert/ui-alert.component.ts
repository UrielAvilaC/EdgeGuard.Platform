import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import {
  faCheckCircle,
  faExclamationTriangle,
  faTimesCircle,
  faInfoCircle,
} from '@fortawesome/free-solid-svg-icons';

export type AlertType = 'success' | 'error' | 'warning' | 'info';

const ALERT_CONFIG: Record<AlertType, { icon: IconProp; classes: string }> = {
  success: { icon: faCheckCircle, classes: 'alert-success' },
  error: { icon: faTimesCircle, classes: 'alert-error' },
  warning: { icon: faExclamationTriangle, classes: 'alert-warning' },
  info: { icon: faInfoCircle, classes: 'alert-info' },
};

@Component({
  selector: 'ui-alert',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FaIconComponent],
  templateUrl: './ui-alert.component.html',
  styleUrl: './ui-alert.component.scss'
})
export class UiAlert {
  readonly type = input<AlertType>('info');
  readonly title = input('');
  readonly dismissible = input(false);
  readonly dismissed = output<void>();

  protected readonly config = computed(() => ALERT_CONFIG[this.type()]);
  protected readonly containerClasses = computed(() => this.config().classes);
}
