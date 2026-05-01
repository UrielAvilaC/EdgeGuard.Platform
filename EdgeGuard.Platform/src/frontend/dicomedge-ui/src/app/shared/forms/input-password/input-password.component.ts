import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faEye, faEyeSlash } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'ui-input-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatInputModule, MatFormFieldModule, FaIconComponent],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => UiInputPassword), multi: true },
  ],
  templateUrl: './input-password.component.html',
  styleUrl: './input-password.component.scss'
})
export class UiInputPassword implements ControlValueAccessor {
  readonly label = input('');
  readonly placeholder = input('');
  readonly hint = input('');
  readonly errorMessage = input('');
  readonly ariaLabel = input('');

  protected readonly visible = signal(false);
  protected readonly iconShow = faEye;
  protected readonly iconHide = faEyeSlash;

  protected value: string = '';
  protected isDisabled = false;

  private onChange: (value: string) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: string): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  protected onValueChange(value: string): void {
    this.value = value;
    this.onChange(value);
  }
}
