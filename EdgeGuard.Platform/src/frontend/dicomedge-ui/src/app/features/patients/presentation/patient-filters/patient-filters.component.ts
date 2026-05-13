import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFilter, faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
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
  templateUrl: './patient-filters.component.html',
  styleUrl: './patient-filters.component.scss'
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
