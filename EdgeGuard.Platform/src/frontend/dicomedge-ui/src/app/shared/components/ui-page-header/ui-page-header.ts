import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-header">
      <div class="page-header-text">
        @if (subtitle()) {
          <p class="page-header-subtitle">{{ subtitle() }}</p>
        }
        <h1 class="page-header-title">{{ title() }}</h1>
        @if (description()) {
          <p class="page-header-description">{{ description() }}</p>
        }
      </div>
      <div class="page-header-actions">
        <ng-content />
      </div>
    </div>
  `,
  styles: `
    .page-header {
      display: flex;
      flex-direction: column;
      gap: 12px;
      margin-bottom: 24px;

      @media (min-width: 640px) {
        flex-direction: row;
        align-items: center;
        justify-content: space-between;
      }
    }

    .page-header-subtitle {
      font-size: 0.6875rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--eg-text-muted);
      margin-bottom: 2px;
    }

    .page-header-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--eg-text-primary);
      letter-spacing: -0.02em;
      line-height: 1.3;
    }

    .page-header-description {
      margin-top: 4px;
      font-size: 0.875rem;
      color: var(--eg-text-secondary);
      max-width: 600px;
    }

    .page-header-actions {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-shrink: 0;
    }
  `,
})
export class UiPageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input('');
  readonly description = input('');
}
