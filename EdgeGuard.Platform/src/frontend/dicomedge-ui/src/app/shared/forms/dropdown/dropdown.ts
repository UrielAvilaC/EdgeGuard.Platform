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
  template: `
    <mat-form-field appearance="outline" class="w-full" subscriptSizing="dynamic">
      @if (label()) {
        <mat-label>{{ label() }}</mat-label>
      }
      <mat-select
        [ngModel]="value"
        (ngModelChange)="onValueChange($event)"
        (blur)="onTouched()"
        [disabled]="isDisabled"
        [placeholder]="placeholder()"
        [multiple]="multiple()"
        [attr.aria-label]="ariaLabel() || label()"
      >
        @if (showEmpty()) {
          <mat-option [value]="null">{{ emptyLabel() }}</mat-option>
        }
        @for (opt of options(); track opt.value) {
          <mat-option [value]="opt.value" [disabled]="opt.disabled ?? false">
            {{ opt.label }}
          </mat-option>
        }
      </mat-select>
      @if (hint()) {
        <mat-hint>{{ hint() }}</mat-hint>
      }
      @if (errorMessage()) {
        <mat-error>{{ errorMessage() }}</mat-error>
      }
    </mat-form-field>
  `,
  styles: `:host { display: block; }`,
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
