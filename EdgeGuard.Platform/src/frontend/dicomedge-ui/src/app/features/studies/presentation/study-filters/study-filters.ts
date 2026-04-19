import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatExpansionModule } from '@angular/material/expansion';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFilter, faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown';
import { UiDateRangePicker, DateRange } from '../../../../shared/forms/date-range-picker/date-range-picker';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle';
import {
  StudyFilter,
  StudyStatus,
  ModalityType,
  STUDY_STATUS_OPTIONS,
  MODALITY_OPTIONS,
} from '../../models/study.models';

@Component({
  selector: 'app-study-filters',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatExpansionModule,
    FontAwesomeModule,
    UiButton,
    UiSearchBar,
    UiDropdown,
    UiDateRangePicker,
    UiSlideToggle,
  ],
  template: `
    <div class="space-y-4">
      <div class="flex items-center gap-4 flex-wrap">
        <div class="flex-1 min-w-[280px]">
          <ui-search-bar
            placeholder="Buscar por accession, paciente, descripción..."
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
            label="Estado"
            [options]="statusOptions"
            [showEmpty]="true"
            emptyLabel="Todos"
            [ngModel]="currentFilter().status ?? null"
            (ngModelChange)="onStatusChange($event)"
          />

          <ui-dropdown
            label="Modalidad"
            [options]="modalityOptions"
            [showEmpty]="true"
            emptyLabel="Todas"
            [ngModel]="currentFilter().modality ?? null"
            (ngModelChange)="onModalityChange($event)"
          />

          <ui-dropdown
            label="Nodo origen"
            [options]="nodeOptions()"
            [showEmpty]="true"
            emptyLabel="Todos"
            [ngModel]="currentFilter().sourceNodeId ?? null"
            (ngModelChange)="onNodeChange($event)"
          />

          <ui-date-range-picker
            label="Rango de fechas"
            [ngModel]="dateRange"
            (ngModelChange)="onDateRangeChange($event)"
          />

          <div class="flex items-end">
            <ui-slide-toggle
              label="Solo urgentes"
              [ngModel]="currentFilter().isUrgent ?? false"
              (ngModelChange)="onUrgentChange($event)"
            />
          </div>
        </div>
      }
    </div>
  `,
})
export class StudyFilters {
  readonly currentFilter = input.required<StudyFilter>();
  readonly activeFilterCount = input(0);
  readonly nodeOptions = input<DropdownOption[]>([]);
  readonly filterChange = output<Partial<StudyFilter>>();
  readonly clearFilters = output<void>();

  protected readonly faFilter = faFilter;
  protected readonly faXmark = faXmark;
  protected readonly statusOptions: DropdownOption<StudyStatus>[] = STUDY_STATUS_OPTIONS;
  protected readonly modalityOptions: DropdownOption<ModalityType>[] = MODALITY_OPTIONS;

  protected filtersExpanded = false;
  protected dateRange: DateRange = { start: null, end: null };

  protected onSearchChange(search: string): void {
    this.filterChange.emit({ search: search || undefined });
  }

  protected onStatusChange(status: StudyStatus | null): void {
    this.filterChange.emit({ status: status ?? undefined });
  }

  protected onModalityChange(modality: ModalityType | null): void {
    this.filterChange.emit({ modality: modality ?? undefined });
  }

  protected onNodeChange(nodeId: string | null): void {
    this.filterChange.emit({ sourceNodeId: nodeId ?? undefined });
  }

  protected onDateRangeChange(range: DateRange): void {
    this.dateRange = range;
    this.filterChange.emit({
      dateFrom: range.start?.toISOString() ?? undefined,
      dateTo: range.end?.toISOString() ?? undefined,
    });
  }

  protected onUrgentChange(isUrgent: boolean): void {
    this.filterChange.emit({ isUrgent: isUrgent || undefined });
  }

  protected onClear(): void {
    this.dateRange = { start: null, end: null };
    this.filtersExpanded = false;
    this.clearFilters.emit();
  }
}
