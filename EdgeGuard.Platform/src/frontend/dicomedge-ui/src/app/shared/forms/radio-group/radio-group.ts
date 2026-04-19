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
  template: `
    @if (label()) {
      <label class="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-300">{{ label() }}</label>
    }
    <mat-radio-group
      [ngModel]="value"
      (ngModelChange)="onValueChange($event)"
      [disabled]="isDisabled"
      [class]="inline() ? 'flex gap-4' : 'flex flex-col gap-2'"
      [attr.aria-label]="ariaLabel() || label()"
    >
      @for (opt of options(); track opt.value) {
        <mat-radio-button [value]="opt.value" [disabled]="opt.disabled ?? false">
          {{ opt.label }}
        </mat-radio-button>
      }
    </mat-radio-group>
  `,
  styles: `:host { display: block; }`,
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
