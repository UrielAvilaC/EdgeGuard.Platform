import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';

export type TrendDirection = 'up' | 'down' | 'neutral';

@Component({
  selector: 'ui-stat-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, FaIconComponent],
  template: `
    <mat-card class="stat-card">
      <mat-card-content class="stat-content">
        <div class="stat-icon-wrapper" [class]="iconBgClass()">
          <fa-icon [icon]="icon()" />
        </div>
        <div class="stat-info">
          <p class="stat-label">{{ label() }}</p>
          <p class="stat-value">{{ value() }}</p>
          @if (trend()) {
            <p class="stat-trend" [class]="trendClasses()">
              {{ trendPrefix() }}{{ trend() }}
            </p>
          }
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    :host { display: block; }

    .stat-card {
      height: 100%;
      transition: box-shadow var(--eg-transition-normal), transform var(--eg-transition-normal);
      cursor: default;

      &:hover {
        box-shadow: var(--eg-shadow-md) !important;
        transform: translateY(-1px);
      }
    }

    .stat-content {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      padding: 20px !important;
    }

    .stat-icon-wrapper {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 48px;
      height: 48px;
      border-radius: 12px;
      flex-shrink: 0;
      font-size: 1.125rem;
      color: #fff;
    }

    .stat-info {
      min-width: 0;
      flex: 1;
    }

    .stat-label {
      font-size: 0.8125rem;
      font-weight: 500;
      color: var(--eg-text-muted);
      margin-bottom: 2px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .stat-value {
      font-size: 1.75rem;
      font-weight: 700;
      color: var(--eg-text-primary);
      letter-spacing: -0.02em;
      line-height: 1.2;
    }

    .stat-trend {
      margin-top: 4px;
      font-size: 0.75rem;
      font-weight: 600;
    }
  `,
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
