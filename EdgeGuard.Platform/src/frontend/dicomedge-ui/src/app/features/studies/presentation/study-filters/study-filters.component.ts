import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatExpansionModule } from '@angular/material/expansion';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFilter, faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiDateRangePicker, DateRange } from '../../../../shared/forms/date-range-picker/date-range-picker.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
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
  templateUrl: './study-filters.component.html',
  styleUrl: './study-filters.component.scss'
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
