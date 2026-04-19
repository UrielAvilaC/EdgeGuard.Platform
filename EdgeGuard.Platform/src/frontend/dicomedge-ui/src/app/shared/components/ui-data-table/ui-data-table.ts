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
import { UiEmptyState } from '../ui-empty-state/ui-empty-state';

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
  template: `
    @if (loading()) {
      <mat-progress-bar mode="indeterminate" class="absolute top-0 left-0 right-0 z-10" />
    }

    <div class="relative overflow-x-auto">
      <table
        mat-table
        [dataSource]="data()"
        matSort
        [matSortActive]="sort()?.sortBy ?? ''"
        [matSortDirection]="sortDirection()"
        (matSortChange)="onSortChange($event)"
        class="w-full"
      >
        @if (selectable()) {
          <ng-container matColumnDef="select">
            <th mat-header-cell *matHeaderCellDef class="w-12">
              <mat-checkbox
                [checked]="isAllSelected()"
                [indeterminate]="selection.hasValue() && !isAllSelected()"
                (change)="toggleAllRows()"
                aria-label="Select all rows"
              />
            </th>
            <td mat-cell *matCellDef="let row">
              <mat-checkbox
                [checked]="selection.isSelected(row)"
                (change)="toggleRow(row)"
                (click)="$event.stopPropagation()"
                aria-label="Select row"
              />
            </td>
          </ng-container>
        }

        @for (col of columns(); track col.key) {
          <ng-container [matColumnDef]="col.key">
            <th
              mat-header-cell
              *matHeaderCellDef
              [mat-sort-header]="col.sortable ? col.key : ''"
              [disabled]="!col.sortable"
              [style.width]="col.width ?? 'auto'"
              [style.text-align]="col.align ?? 'left'"
              class="font-semibold text-sm text-gray-600 dark:text-gray-300"
            >
              {{ col.header }}
            </th>
            <td
              mat-cell
              *matCellDef="let row"
              [style.text-align]="col.align ?? 'left'"
              class="text-sm"
            >
              @if (getCellTemplate(col.key); as tmpl) {
                <ng-container
                  [ngTemplateOutlet]="tmpl"
                  [ngTemplateOutletContext]="{ $implicit: row, value: getCellValue(row, col) }"
                />
              } @else {
                {{ getCellValue(row, col) }}
              }
            </td>
          </ng-container>
        }

        <tr mat-header-row *matHeaderRowDef="displayedColumns()" class="bg-gray-50 dark:bg-gray-800"></tr>
        <tr
          mat-row
          *matRowDef="let row; columns: displayedColumns()"
          (click)="rowClick.emit(row)"
          class="hover:bg-gray-50 dark:hover:bg-gray-800 cursor-pointer transition-colors"
        ></tr>
      </table>
    </div>

    @if (!loading() && data().length === 0) {
      <ui-empty-state [title]="emptyMessage()" />
    }

    @if (pagination()) {
      <mat-paginator
        [length]="pagination()!.total"
        [pageIndex]="pagination()!.page - 1"
        [pageSize]="pagination()!.pageSize"
        [pageSizeOptions]="pageSizeOptions()"
        (page)="onPageChange($event)"
        showFirstLastButtons
        aria-label="Paginación"
      />
    }
  `,
  styles: `
    :host {
      display: block;
      position: relative;
    }

    .relative {
      border-radius: var(--eg-radius-lg);
      overflow: hidden;
    }

    table {
      border-collapse: separate;
      border-spacing: 0;
    }

    th.mat-mdc-header-cell {
      background: var(--eg-surface-dim);
      border-bottom: 2px solid var(--eg-border);
      font-weight: 600;
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--eg-text-muted);
      padding: 12px 16px;
    }

    td.mat-mdc-cell {
      border-bottom: 1px solid var(--eg-border-subtle);
      padding: 12px 16px;
      color: var(--eg-text-primary);
    }

    tr.mat-mdc-row {
      transition: background-color var(--eg-transition-fast);

      &:hover {
        background: var(--eg-surface-container) !important;
      }
    }
  `,
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
