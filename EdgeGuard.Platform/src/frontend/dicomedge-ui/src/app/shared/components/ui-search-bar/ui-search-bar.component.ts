import { ChangeDetectionStrategy, Component, input, output, signal, OnInit, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faSearch, faTimes } from '@fortawesome/free-solid-svg-icons';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';

@Component({
  selector: 'ui-search-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatInputModule, MatFormFieldModule, FaIconComponent],
  templateUrl: './ui-search-bar.component.html',
  styleUrl: './ui-search-bar.component.scss'
})
export class UiSearchBar implements OnInit, OnDestroy {
  readonly placeholder = input('Buscar...');
  readonly debounce = input(300);
  readonly searchChange = output<string>();

  protected readonly value = signal('');
  protected readonly searchIcon = faSearch;
  protected readonly clearIcon = faTimes;

  private readonly input$ = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.input$.pipe(
      debounceTime(this.debounce()),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe((value) => this.searchChange.emit(value));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  protected onInput(value: string): void {
    this.value.set(value);
    this.input$.next(value);
  }

  protected clear(): void {
    this.value.set('');
    this.input$.next('');
  }
}
