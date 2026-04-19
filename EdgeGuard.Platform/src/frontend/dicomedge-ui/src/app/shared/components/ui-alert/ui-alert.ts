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
  template: `
    <div
      [class]="containerClasses()"
      role="alert"
      class="alert-container"
    >
      <fa-icon [icon]="config().icon" class="alert-icon" />
      <div class="alert-body">
        @if (title()) {
          <p class="alert-title">{{ title() }}</p>
        }
        <div class="alert-content">
          <ng-content />
        </div>
      </div>
      @if (dismissible()) {
        <button
          type="button"
          class="alert-dismiss"
          aria-label="Cerrar"
          (click)="dismissed.emit()"
        >
          &times;
        </button>
      }
    </div>
  `,
  styles: `
    .alert-container {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 14px 16px;
      border-radius: var(--eg-radius-md);
      border-left: 3px solid;
      font-size: 0.875rem;
    }

    .alert-icon {
      flex-shrink: 0;
      margin-top: 1px;
      font-size: 1rem;
    }

    .alert-body {
      flex: 1;
      min-width: 0;
    }

    .alert-title {
      font-weight: 600;
      margin-bottom: 2px;
    }

    .alert-content {
      line-height: 1.5;
    }

    .alert-dismiss {
      flex-shrink: 0;
      background: none;
      border: none;
      font-size: 1.25rem;
      cursor: pointer;
      opacity: 0.5;
      transition: opacity var(--eg-transition-fast);
      line-height: 1;
      padding: 0 2px;
      color: inherit;

      &:hover {
        opacity: 1;
      }
    }

    .alert-success {
      background: #f0fdf4;
      border-color: #22c55e;
      color: #166534;
    }

    .alert-error {
      background: #fef2f2;
      border-color: #ef4444;
      color: #991b1b;
    }

    .alert-warning {
      background: #fffbeb;
      border-color: #f59e0b;
      color: #92400e;
    }

    .alert-info {
      background: #eff6ff;
      border-color: #0078d4;
      color: #1e40af;
    }

    :host-context(.dark) {
      .alert-success { background: rgba(34, 197, 94, 0.1); color: #86efac; }
      .alert-error   { background: rgba(239, 68, 68, 0.1);  color: #fca5a5; }
      .alert-warning { background: rgba(245, 158, 11, 0.1); color: #fcd34d; }
      .alert-info    { background: rgba(0, 120, 212, 0.1);  color: #93c5fd; }
    }
  `,
})
export class UiAlert {
  readonly type = input<AlertType>('info');
  readonly title = input('');
  readonly dismissible = input(false);
  readonly dismissed = output<void>();

  protected readonly config = computed(() => ALERT_CONFIG[this.type()]);
  protected readonly containerClasses = computed(() => this.config().classes);
}
