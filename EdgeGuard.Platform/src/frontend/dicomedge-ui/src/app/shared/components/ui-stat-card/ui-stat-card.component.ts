import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';

export type TrendDirection = 'up' | 'down' | 'neutral';

@Component({
  selector: 'ui-stat-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, FaIconComponent],
  templateUrl: './ui-stat-card.component.html',
  styleUrl: './ui-stat-card.component.scss'
})
export class UiStatCard {
  readonly icon = input.required<IconProp>();
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly trend = input<string>('');
  readonly trendDirection = input<TrendDirection>('neutral');
  readonly iconBgClass = input('bg-azure-50');

  protected readonly trendClasses = computed(() => {
    switch (this.trendDirection()) {
      case 'up': return 'text-green-600 dark:text-green-400';
      case 'down': return 'text-red-600 dark:text-red-400';
      default: return 'text-gray-500 dark:text-gray-400';
    }
  });

  protected readonly trendPrefix = computed(() => {
    switch (this.trendDirection()) {
      case 'up': return '\u2191 ';
      case 'down': return '\u2193 ';
      default: return '';
    }
  });
}
