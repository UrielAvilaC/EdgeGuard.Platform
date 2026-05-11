import { computed, Injectable, signal } from '@angular/core';

import { PaginationMeta } from '../../../shared/models/pagination.model';
import { SortParams } from '../../../shared/models/sort.model';
import { PacsServer, PacsServerFilter } from '../models/pacs.models';

@Injectable()
export class PacsStore {
  // ── Private state ──
  private readonly _servers = signal<PacsServer[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _pagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _sort = signal<SortParams>({ sortBy: 'name', sortDir: 'asc' });
  private readonly _filter = signal<PacsServerFilter>({});

  // ── Public readonly ──
  readonly servers = this._servers.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly pagination = this._pagination.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly filter = this._filter.asReadonly();

  // ── Computed ──
  readonly hasData = computed(() => this._servers().length > 0);

  readonly activeFilterCount = computed(() => {
    const f = this._filter();
    let count = 0;
    if (f.search) count++;
    if (f.isEnabled !== undefined) count++;
    if (f.isGlobal !== undefined) count++;
    return count;
  });

  readonly reachableCount = computed(() =>
    this._servers().filter(s => s.isReachable).length,
  );

  // ── Mutations ──
  setServers(servers: PacsServer[], total: number): void {
    this._servers.set(servers);
    this._pagination.update(p => ({ ...p, total }));
    this._error.set(null);
  }

  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setError(error: string | null): void {
    this._error.set(error);
  }

  setPagination(page: number, pageSize: number): void {
    this._pagination.update(p => ({ ...p, page, pageSize }));
  }

  setSort(sort: SortParams): void {
    this._sort.set(sort);
  }

  updateFilter(partial: Partial<PacsServerFilter>): void {
    this._filter.update(f => ({ ...f, ...partial }));
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  clearFilters(): void {
    this._filter.set({});
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  removeServerFromList(id: string): void {
    this._servers.update(list => list.filter(s => s.id !== id));
  }
}
