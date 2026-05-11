import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatExpansionModule } from '@angular/material/expansion';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFilter, faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { NodeFilter, NodeStatus, NODE_STATUS_OPTIONS } from '../../models/node.models';

@Component({
  selector: 'app-node-filters',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatExpansionModule,
    FontAwesomeModule,
    UiButton,
    UiSearchBar,
    UiDropdown,
    UiSlideToggle,
  ],
  templateUrl: './node-filters.component.html',
  styleUrl: './node-filters.component.scss'
})
export class NodeFilters {
  readonly currentFilter = input.required<NodeFilter>();
  readonly activeFilterCount = input(0);
  readonly filterChange = output<Partial<NodeFilter>>();
  readonly clearFilters = output<void>();

  protected readonly faFilter = faFilter;
  protected readonly faXmark = faXmark;
  protected readonly statusOptions: DropdownOption<NodeStatus>[] = NODE_STATUS_OPTIONS;

  protected filtersExpanded = false;

  protected onSearchChange(search: string): void {
    this.filterChange.emit({ search: search || undefined });
  }

  protected onStatusChange(status: NodeStatus | null): void {
    this.filterChange.emit({ status: status ?? undefined });
  }

  protected onEnabledChange(isEnabled: boolean): void {
    this.filterChange.emit({ isEnabled: isEnabled || undefined });
  }

  protected onClear(): void {
    this.filtersExpanded = false;
    this.clearFilters.emit();
  }
}
