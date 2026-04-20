import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faPlus,
  faPen,
  faToggleOn,
  faToggleOff,
  faTrash,
  faFilter,
  faXmark,
} from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { PaginationMeta } from '../../../../shared/models/pagination.model';
import { SortParams } from '../../../../shared/models/sort.model';
import { RoutingRule, RoutingRuleFilter } from '../../models/hl7.models';

@Component({
  selector: 'app-routing-rules-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiDataTable,
    UiCellDef,
    UiChip,
    UiSearchBar,
    UiSlideToggle,
    RelativeTimePipe,
  ],
  templateUrl: './routing-rules-list.component.html',
  styleUrl: './routing-rules-list.component.scss'
})
export class RoutingRulesList {
  readonly rules = input.required<RoutingRule[]>();
  readonly pagination = input.required<PaginationMeta>();
  readonly sort = input.required<SortParams>();
  readonly filter = input.required<RoutingRuleFilter>();
  readonly loading = input(false);
  readonly activeFilterCount = input(0);

  readonly create = output<void>();
  readonly edit = output<RoutingRule>();
  readonly enable = output<string>();
  readonly disable = output<string>();
  readonly deleteRule = output<RoutingRule>();
  readonly searchChange = output<string | undefined>();
  readonly clearFilters = output<void>();
  readonly pageChange = output<{ page: number; pageSize: number }>();
  readonly sortChange = output<SortParams>();

  protected readonly faPlus = faPlus;
  protected readonly faPen = faPen;
  protected readonly faToggleOn = faToggleOn;
  protected readonly faToggleOff = faToggleOff;
  protected readonly faTrash = faTrash;
  protected readonly faFilter = faFilter;
  protected readonly faXmark = faXmark;

  protected filtersExpanded = false;

  protected readonly columns: TableColumn<RoutingRule>[] = [
    { key: 'priority', header: 'Prioridad', sortable: true, width: '8%', align: 'center' },
    { key: 'name', header: 'Nombre', sortable: true, width: '22%' },
    { key: 'matchers', header: 'Criterios', width: '24%' },
    { key: 'targetNodeId', header: 'Nodo destino', sortable: true, width: '14%' },
    { key: 'matchCount', header: 'Matches', sortable: true, width: '8%', align: 'center' },
    { key: 'lastMatchedAt', header: 'Último match', sortable: true, width: '12%' },
    { key: 'actions', header: '', width: '12%' },
  ];

  protected onSearchChange(search: string): void {
    this.searchChange.emit(search || undefined);
  }

  protected onEnabledChange(isEnabled: boolean): void {
    this.searchChange.emit(undefined); // triggers parent updateFilter
  }
}
