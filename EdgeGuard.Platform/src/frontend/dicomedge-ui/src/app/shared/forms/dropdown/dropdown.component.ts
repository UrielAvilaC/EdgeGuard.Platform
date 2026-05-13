import { ChangeDetectionStrategy, Component, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';

export interface DropdownOption<T = string> {
  value: T;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'ui-dropdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatSelectModule, MatFormFieldModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => UiDropdown), multi: true },
  ],
  templateUrl: './dropdown.component.html',
  styleUrl: './dropdown.component.scss'
})
export class UiDropdown<T = string> implements ControlValueAccessor {
  readonly label = input('');
  readonly placeholder = input('Seleccionar...');
  readonly hint = input('');
  readonly errorMessage = input('');
  readonly ariaLabel = input('');
  readonly options = input<DropdownOption<T>[]>([]);
  readonly multiple = input(false);
  readonly showEmpty = input(false);
  readonly emptyLabel = input('-- Ninguno --');

  protected value: T | T[] | null = null;
  protected isDisabled = false;

  private onChange: (value: T | T[] | null) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: T | T[] | null): void {
    this.value = value;
  }

  registerOnChange(fn: (value: T | T[] | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  protected onValueChange(value: T | T[] | null): void {
    this.value = value;
    this.onChange(value);
  }
}
