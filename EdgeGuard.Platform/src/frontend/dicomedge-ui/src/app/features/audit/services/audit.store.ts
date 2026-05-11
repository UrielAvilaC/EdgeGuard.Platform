import { computed, Injectable, signal } from '@angular/core';

import { PaginationMeta } from '../../../shared/models/pagination.model';
import { SortParams } from '../../../shared/models/sort.model';
import { AuditLogDto, AuditLogFilter } from '../models/audit.models';

@Injectable()
export class AuditStore {
  private readonly _logs = signal<AuditLogDto[]>([]);
  private readonly _selectedLog = signal<AuditLogDto | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _pagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _sort = signal<SortParams>({ sortBy: 'createdAt', sortDir: 'desc' });
  private readonly _filter = signal<AuditLogFilter>({});
  private readonly _eventTypes = signal<string[]>([]);

  readonly logs = this._logs.asReadonly();
  readonly selectedLog = this._selectedLog.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly pagination = this._pagination.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly filter = this._filter.asReadonly();
  readonly eventTypes = this._eventTypes.asReadonly();

  readonly hasData = computed(() => this._logs().length > 0);

  readonly activeFilterCount = computed(() => {
    const f = this._filter();
    let count = 0;
    if (f.eventType) count++;
    if (f.severity) count++;
    if (f.userId) count++;
    if (f.entityType) count++;
    if (f.dateFrom) count++;
    if (f.dateTo) count++;
    if (f.isSuccess !== undefined) count++;
    if (f.search) count++;
    return count;
  });

  setLogs(logs: AuditLogDto[], total: number): void {
    this._logs.set(logs);
    this._pagination.update(p => ({ ...p, total }));
    this._error.set(null);
  }

  setSelectedLog(log: AuditLogDto | null): void { this._selectedLog.set(log); }
  setLoading(loading: boolean): void { this._loading.set(loading); }
  setError(error: string | null): void { this._error.set(error); }
  setEventTypes(types: string[]): void { this._eventTypes.set(types); }

  setPagination(page: number, pageSize: number): void {
    this._pagination.update(p => ({ ...p, page, pageSize }));
  }

  setSort(sort: SortParams): void { this._sort.set(sort); }

  updateFilter(partial: Partial<AuditLogFilter>): void {
    this._filter.update(f => ({ ...f, ...partial }));
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  clearFilters(): void {
    this._filter.set({});
    this._pagination.update(p => ({ ...p, page: 1 }));
  }
}
