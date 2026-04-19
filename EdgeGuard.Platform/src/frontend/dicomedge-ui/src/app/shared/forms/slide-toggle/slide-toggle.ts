import { ChangeDetectionStrategy, Component, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

@Component({
  selector: 'ui-slide-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatSlideToggleModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => UiSlideToggle), multi: true },
  ],
  template: `
    <mat-slide-toggle
      [ngModel]="value"
      (ngModelChange)="onValueChange($event)"
      [disabled]="isDisabled"
      [attr.aria-label]="ariaLabel() || label()"
    >
      {{ label() }}
    </mat-slide-toggle>
  `,
  styles: `:host { display: block; }`,
})
export class UiSlideToggle implements ControlValueAccessor {
  readonly label = input('');
  readonly ariaLabel = input('');

  protected value = false;
  protected isDisabled = false;

  private onChange: (value: boolean) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: boolean): void {
    this.value = !!value;
  }

  registerOnChange(fn: (value: boolean) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  protected onValueChange(value: boolean): void {
    this.value = value;
    this.onChange(value);
  }
}
