import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { faInbox } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'ui-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FaIconComponent],
  template: `
    <div class="empty-state">
      <div class="empty-state-icon">
        <fa-icon [icon]="icon()" />
      </div>
      <h3 class="empty-state-title">{{ title() }}</h3>
      @if (description()) {
        <p class="empty-state-desc">{{ description() }}</p>
      }
      <ng-content />
    </div>
  `,
  styles: `
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      text-align: center;
    }

    .empty-state-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 64px;
      height: 64px;
      border-radius: 50%;
      background: var(--eg-surface-container);
      color: var(--eg-text-muted);
      font-size: 1.5rem;
      margin-bottom: 16px;
    }

    .empty-state-title {
      font-size: 1rem;
      font-weight: 600;
      color: var(--eg-text-secondary);
      margin-bottom: 4px;
    }

    .empty-state-desc {
      font-size: 0.875rem;
      color: var(--eg-text-muted);
      max-width: 360px;
      margin-bottom: 16px;
    }
  `,
})
export class UiEmptyState {
  readonly icon = input<IconProp>(faInbox);
  readonly title = input('No hay datos');
  readonly description = input('');
}
