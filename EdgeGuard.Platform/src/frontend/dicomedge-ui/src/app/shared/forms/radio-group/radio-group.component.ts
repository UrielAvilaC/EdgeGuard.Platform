import { ChangeDetectionStrategy, Component, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatRadioModule } from '@angular/material/radio';

export interface RadioOption<T = string> {
  value: T;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'ui-radio-group',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatRadioModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => UiRadioGroup), multi: true },
  ],
  templateUrl: './radio-group.component.html',
  styleUrl: './radio-group.component.scss'
})
export class UiRadioGroup<T = string> implements ControlValueAccessor {
  readonly label = input('');
  readonly ariaLabel = input('');
  readonly options = input<RadioOption<T>[]>([]);
  readonly inline = input(false);

  protected value: T | null = null;
  protected isDisabled = false;

  private onChange: (value: T | null) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: T | null): void {
    this.value = value;
  }

  registerOnChange(fn: (value: T | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  protected onValueChange(value: T | null): void {
    this.value = value;
    this.onChange(value);
  }
}
