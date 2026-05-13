import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ContentChildren,
  Directive,
  QueryList,
  TemplateRef,
  computed,
  input,
  output,
} from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { SelectionModel } from '@angular/cdk/collections';

import { TableColumn } from '../../models/table.model';
import { PaginationMeta } from '../../models/pagination.model';
import { SortParams } from '../../models/sort.model';
import { UiEmptyState } from '../ui-empty-state/ui-empty-state.component';

@Directive({
  selector: '[uiCellDef]',
})
export class UiCellDef<T = unknown> {
  readonly columnKey = input.required<string>({ alias: 'uiCellDef' });

  constructor(public readonly templateRef: TemplateRef<{ $implicit: T; value: unknown }>) {}
}

@Component({
  selector: 'ui-data-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgTemplateOutlet,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatCheckboxModule,
    MatProgressBarModule,
    UiEmptyState,
  ],
  templateUrl: './ui-data-table.component.html',
  styleUrl: './ui-data-table.component.scss'
})
export class UiDataTable<T> {
  readonly columns = input.required<TableColumn<T>[]>();
  readonly data = input.required<T[]>();
  readonly pagination = input<PaginationMeta | null>(null);
  readonly sort = input<SortParams | null>(null);
  readonly loading = input(false);
  readonly emptyMessage = input('No se encontraron registros.');
  readonly selectable = input(false);
  readonly pageSizeOptions = input([10, 25, 50, 100]);

  readonly pageChange = output<{ page: number; pageSize: number }>();
  readonly sortChange = output<SortParams>();
  readonly rowClick = output<T>();
  readonly selectionChange = output<T[]>();

  @ContentChildren(UiCellDef) cellDefs!: QueryList<UiCellDef<T>>;

  readonly selection = new SelectionModel<T>(true, []);

  protected readonly displayedColumns = computed(() => {
    const cols = this.columns().map((c) => c.key);
    return this.selectable() ? ['select', ...cols] : cols;
  });

  protected readonly sortDirection = computed(() => {
    const dir = this.sort()?.sortDir;
    if (dir === 'asc' || dir === 'desc') return dir;
    return '';
  });

  protected getCellTemplate(key: string): TemplateRef<{ $implicit: T; value: unknown }> | null {
    return this.cellDefs?.find((d) => d.columnKey() === key)?.templateRef ?? null;
  }

  protected getCellValue(row: T, col: TableColumn<T>): unknown {
    if (col.valueAccessor) return col.valueAccessor(row);
    return (row as Record<string, unknown>)[col.key] ?? '';
  }

  protected isAllSelected(): boolean {
    return this.selection.selected.length === this.data().length && this.data().length > 0;
  }

  protected toggleAllRows(): void {
    if (this.isAllSelected()) {
      this.selection.clear();
    } else {
      this.selection.select(...this.data());
    }
    this.selectionChange.emit(this.selection.selected);
  }

  protected toggleRow(row: T): void {
    this.selection.toggle(row);
    this.selectionChange.emit(this.selection.selected);
  }

  protected onSortChange(sort: Sort): void {
    this.sortChange.emit({
      sortBy: sort.active,
      sortDir: sort.direction === 'desc' ? 'desc' : 'asc',
    });
  }

  protected onPageChange(event: PageEvent): void {
    this.pageChange.emit({
      page: event.pageIndex + 1,
      pageSize: event.pageSize,
    });
  }
}
