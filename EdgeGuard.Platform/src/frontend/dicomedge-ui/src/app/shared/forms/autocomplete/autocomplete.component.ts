import { ChangeDetectionStrategy, Component, forwardRef, input, signal, OnInit, OnDestroy } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';

export interface AutocompleteOption<T = string> {
  value: T;
  label: string;
}

@Component({
  selector: 'ui-autocomplete',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatAutocompleteModule, MatInputModule, MatFormFieldModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => UiAutocomplete), multi: true },
  ],
  templateUrl: './autocomplete.component.html',
  styleUrl: './autocomplete.component.scss'
})
export class UiAutocomplete<T = string> implements ControlValueAccessor, OnInit, OnDestroy {
  readonly label = input('');
  readonly placeholder = input('');
  readonly hint = input('');
  readonly ariaLabel = input('');
  readonly options = input<AutocompleteOption<T>[]>([]);
  readonly debounceMs = input(200);

  protected readonly displayValue = signal('');
  protected readonly filteredOptions = signal<AutocompleteOption<T>[]>([]);

  protected isDisabled = false;

  private selectedValue: T | null = null;
  private onChange: (value: T | null) => void = () => {};
  protected onTouched: () => void = () => {};
  private readonly input$ = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.filteredOptions.set(this.options());
    this.input$.pipe(
      debounceTime(this.debounceMs()),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe((text) => {
      const lower = text.toLowerCase();
      this.filteredOptions.set(
        this.options().filter((o) => o.label.toLowerCase().includes(lower)),
      );
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  writeValue(value: T | null): void {
    this.selectedValue = value;
    const match = this.options().find((o) => o.value === value);
    this.displayValue.set(match?.label ?? '');
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

  protected onInputChange(text: string): void {
    this.displayValue.set(text);
    this.input$.next(text);
  }

  protected onOptionSelected(value: T): void {
    this.selectedValue = value;
    const match = this.options().find((o) => o.value === value);
    this.displayValue.set(match?.label ?? '');
    this.onChange(value);
  }
}
