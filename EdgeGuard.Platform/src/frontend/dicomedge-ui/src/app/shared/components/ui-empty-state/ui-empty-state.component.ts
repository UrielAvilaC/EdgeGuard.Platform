import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { faInbox } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'ui-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FaIconComponent],
  templateUrl: './ui-empty-state.component.html',
  styleUrl: './ui-empty-state.component.scss'
})
export class UiEmptyState {
  readonly icon = input<IconProp>(faInbox);
  readonly title = input('No hay datos');
  readonly description = input('');
}
