import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { UiChip, ChipColor } from '../ui-chip/ui-chip';

export type StatusColorMap = Record<string, ChipColor>;

const DEFAULT_STATUS_COLORS: StatusColorMap = {
  active: 'success',
  online: 'success',
  healthy: 'success',
  completed: 'success',
  delivered: 'success',
  enabled: 'success',
  warning: 'warning',
  pending: 'warning',
  queued: 'warning',
  processing: 'info',
  dispatching: 'info',
  sending: 'info',
  error: 'danger',
  failed: 'danger',
  offline: 'danger',
  disabled: 'danger',
  inactive: 'default',
  unknown: 'default',
};

@Component({
  selector: 'ui-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [UiChip],
  template: `<ui-chip [color]="chipColor()">{{ label() || status() }}</ui-chip>`,
  styles: `:host { display: inline-block; }`,
})
export class UiStatusBadge {
  readonly status = input.required<string>();
  readonly label = input('');
  readonly colorMap = input<StatusColorMap>({});

  protected readonly chipColor = computed<ChipColor>(() => {
    const s = this.status().toLowerCase();
    return this.colorMap()[s] ?? DEFAULT_STATUS_COLORS[s] ?? 'default';
  });
}
