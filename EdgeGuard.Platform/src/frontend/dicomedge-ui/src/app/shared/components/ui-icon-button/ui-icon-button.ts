import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';

@Component({
  selector: 'ui-icon-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconButton, MatTooltipModule, FaIconComponent],
  template: `
    <button
      mat-icon-button
      [matTooltip]="tooltip()"
      [disabled]="disabled()"
      [attr.aria-label]="ariaLabel() || tooltip()"
      (click)="clicked.emit($event)"
    >
      <fa-icon [icon]="icon()" />
    </button>
  `,
  styles: `:host { display: inline-block; }`,
})
export class UiIconButton {
  readonly icon = input.required<IconProp>();
  readonly tooltip = input('');
  readonly ariaLabel = input('');
  readonly disabled = input(false);
  readonly clicked = output<MouseEvent>();
}
