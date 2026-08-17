import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';

/**
 * Labelled description field (`<dt>`/`<dd>` pair) used inside detail cards.
 * The value is projected, so callers can pass plain text, links, chips, etc.
 * Use `inline` for compact rows (label left, value right) such as timestamps.
 */
@Component({
  selector: 'ui-detail-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FaIconComponent],
  template: `
    @if (inline()) {
      <div class="flex items-center justify-between gap-2">
        <dt class="text-sm text-gray-500 dark:text-gray-400">{{ label() }}</dt>
        <dd class="text-sm text-gray-900 dark:text-white" [class.font-mono]="mono()">
          <ng-content />
        </dd>
      </div>
    } @else {
      <div>
        <dt class="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-1.5">
          @if (icon(); as ic) {
            <fa-icon [icon]="ic" aria-hidden="true" />
          }
          <span>{{ label() }}</span>
        </dt>
        <dd class="mt-1 text-sm text-gray-900 dark:text-white" [class.font-mono]="mono()">
          <ng-content />
        </dd>
      </div>
    }
  `,
})
export class UiDetailItem {
  readonly label = input.required<string>();
  readonly icon = input<IconProp>();
  readonly mono = input(false);
  readonly inline = input(false);
}
