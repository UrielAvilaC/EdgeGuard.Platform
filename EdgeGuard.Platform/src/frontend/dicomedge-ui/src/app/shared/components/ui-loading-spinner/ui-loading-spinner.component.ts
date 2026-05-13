import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'ui-loading-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatProgressSpinnerModule],
  templateUrl: './ui-loading-spinner.component.html',
  styleUrl: './ui-loading-spinner.component.scss'
})
export class UiLoadingSpinner {
  readonly overlay = input(false);
  readonly diameter = input(40);
}
