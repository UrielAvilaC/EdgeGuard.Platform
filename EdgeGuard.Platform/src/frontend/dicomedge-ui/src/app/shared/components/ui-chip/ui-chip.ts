import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';

export type ChipColor = 'default' | 'primary' | 'success' | 'warning' | 'danger' | 'info';

const CHIP_CLASSES: Record<ChipColor, string> = {
  default: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
  primary: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
  success: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
  warning: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300',
  danger: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300',
  info: 'bg-cyan-100 text-cyan-800 dark:bg-cyan-900 dark:text-cyan-300',
};

@Component({
  selector: 'ui-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatChipsModule],
  template: `
    <span
      [class]="classes()"
      class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
    >
      <ng-content />
    </span>
  `,
  styles: `:host { display: inline-block; }`,
})
export class UiChip {
  readonly color = input<ChipColor>('default');

  protected readonly classes = computed(() => CHIP_CLASSES[this.color()]);
}
