import { ChangeDetectionStrategy, Component, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatCheckboxModule } from '@angular/material/checkbox';

@Component({
  selector: 'ui-checkbox',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatCheckboxModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => UiCheckbox), multi: true },
  ],
  template: `
    <mat-checkbox
      [ngModel]="value"
      (ngModelChange)="onValueChange($event)"
      [disabled]="isDisabled"
      (blur)="onTouched()"
      [attr.aria-label]="ariaLabel() || label()"
    >
      {{ label() }}
    </mat-checkbox>
  `,
  styles: `:host { display: block; }`,
})
export class UiCheckbox implements ControlValueAccessor {
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
