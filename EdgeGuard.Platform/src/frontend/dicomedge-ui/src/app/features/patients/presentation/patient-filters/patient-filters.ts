import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFilter, faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle';
import { PatientFilter } from '../../models/patient.models';

@Component({
  selector: 'app-patient-filters',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    FontAwesomeModule,
    UiButton,
    UiSearchBar,
    UiDropdown,
    UiSlideToggle,
  ],
  template: `
    <div class="space-y-4">
      <div class="flex items-center gap-4 flex-wrap">
        <div class="flex-1 min-w-[280px]">
          <ui-search-bar
            placeholder="Buscar por nombre o DICOM ID..."
            (searchChange)="onSearchChange($event)"
          />
        </div>

        <ui-button
          variant="secondary"
          size="sm"
          [icon]="faFilter"
          (clicked)="filtersExpanded = !filtersExpanded"
        >
          Filtros
          @if (activeFilterCount() > 0) {
            <span class="ml-1.5 inline-flex items-center justify-center w-5 h-5 text-xs font-bold rounded-full bg-azure-600 text-white">
              {{ activeFilterCount() }}
            </span>
          }
        </ui-button>

        @if (activeFilterCount() > 0) {
          <ui-button variant="ghost" size="sm" [icon]="faXmark" (clicked)="onClear()">
            Limpiar
          </ui-button>
        }
      </div>

      @if (filtersExpanded) {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 p-4 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50"
             role="group" aria-label="Filtros avanzados">

          <ui-dropdown
            label="Nodo origen"
            [options]="nodeOptions()"
            [showEmpty]="true"
            emptyLabel="Todos"
            [ngModel]="currentFilter().createdByNodeId ?? null"
            (ngModelChange)="onNodeChange($event)"
          />

          <ui-dropdown
            label="Estado"
            [options]="activeOptions"
            [showEmpty]="true"
            emptyLabel="Todos"
            [ngModel]="activeValue()"
            (ngModelChange)="onActiveChange($event)"
          />

          <div class="flex items-end">
            <ui-slide-toggle
              label="Con teléfono"
              [ngModel]="currentFilter().hasPhone ?? false"
              (ngModelChange)="onHasPhoneChange($event)"
            />
          </div>

          <div class="flex items-end">
            <ui-slide-toggle
              label="Con email"
              [ngModel]="currentFilter().hasEmail ?? false"
              (ngModelChange)="onHasEmailChange($event)"
            />
          </div>
        </div>
      }
    </div>
  `,
})
export class PatientFilters {
  readonly currentFilter = input.required<PatientFilter>();
  readonly activeFilterCount = input(0);
  readonly nodeOptions = input<DropdownOption[]>([]);
  readonly filterChange = output<Partial<PatientFilter>>();
  readonly clearFilters = output<void>();

  protected readonly faFilter = faFilter;
  protected readonly faXmark = faXmark;

  protected readonly activeOptions: DropdownOption<string>[] = [
    { value: 'true', label: 'Activos' },
    { value: 'false', label: 'Inactivos' },
  ];

  protected filtersExpanded = false;

  protected activeValue(): string | null {
    const v = this.currentFilter().isActive;
    return v === undefined ? null : String(v);
  }

  protected onSearchChange(search: string): void {
    this.filterChange.emit({ search: search || undefined });
  }

  protected onNodeChange(nodeId: string | null): void {
    this.filterChange.emit({ createdByNodeId: nodeId ?? undefined });
  }

  protected onActiveChange(value: string | null): void {
    this.filterChange.emit({ isActive: value === null ? undefined : value === 'true' });
  }

  protected onHasPhoneChange(hasPhone: boolean): void {
    this.filterChange.emit({ hasPhone: hasPhone || undefined });
  }

  protected onHasEmailChange(hasEmail: boolean): void {
    this.filterChange.emit({ hasEmail: hasEmail || undefined });
  }

  protected onClear(): void {
    this.filtersExpanded = false;
    this.clearFilters.emit();
  }
}
