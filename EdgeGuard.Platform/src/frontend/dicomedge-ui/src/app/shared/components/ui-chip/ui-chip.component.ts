import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type ChipColor = 'default' | 'primary' | 'success' | 'warning' | 'danger' | 'info';

@Component({
  selector: 'ui-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ui-chip.component.html',
  styleUrl: './ui-chip.component.scss'
})
export class UiChip {
  readonly color = input<ChipColor>('default');

  protected readonly colorClass = computed(() => `chip-${this.color()}`);
}
