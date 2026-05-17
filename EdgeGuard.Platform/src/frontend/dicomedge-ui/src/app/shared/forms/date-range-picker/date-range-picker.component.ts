import { ChangeDetectionStrategy, Component, forwardRef, input, output } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';

export interface DateRange {
  start: Date | null;
  end: Date | null;
}

@Component({
  selector: 'ui-date-range-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatDatepickerModule, MatInputModule, MatFormFieldModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => UiDateRangePicker), multi: true },
  ],
  templateUrl: './date-range-picker.component.html',
  styleUrl: './date-range-picker.component.scss'
})
export class UiDateRangePicker implements ControlValueAccessor {
  readonly label = input('');
  readonly startPlaceholder = input('Inicio');
  readonly endPlaceholder = input('Fin');
  readonly hint = input('');

  protected value: DateRange = { start: null, end: null };
  protected isDisabled = false;

  private onChange: (value: DateRange) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: DateRange | null): void {
    this.value = value ?? { start: null, end: null };
  }

  registerOnChange(fn: (value: DateRange) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  protected onStartChange(start: Date | null): void {
    this.value = { ...this.value, start };
    this.onChange(this.value);
  }

  protected onEndChange(end: Date | null): void {
    this.value = { ...this.value, end };
    this.onChange(this.value);
  }
}
