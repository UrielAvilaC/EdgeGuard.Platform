import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ui-page-header.component.html',
  styleUrl: './ui-page-header.component.scss'
})
export class UiPageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input('');
  readonly description = input('');
}
