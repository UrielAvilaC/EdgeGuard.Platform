import { computed, Injectable, signal } from '@angular/core';

import { PaginationMeta } from '../../../shared/models/pagination.model';
import { SortParams } from '../../../shared/models/sort.model';
import { RoutingRule, RoutingRuleFilter } from '../models/routing-rule.models';

@Injectable()
export class RoutingRulesStore {
  // ── Private state ──
  private readonly _rules = signal<RoutingRule[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _pagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _sort = signal<SortParams>({ sortBy: 'priority', sortDir: 'asc' });
  private readonly _filter = signal<RoutingRuleFilter>({});

  // ── Public readonly ──
  readonly rules = this._rules.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly pagination = this._pagination.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly filter = this._filter.asReadonly();

  // ── Computed ──
  readonly hasData = computed(() => this._rules().length > 0);

  readonly activeFilterCount = computed(() => {
    const f = this._filter();
    let count = 0;
    if (f.search) count++;
    if (f.isEnabled !== undefined) count++;
    if (f.targetNodeId) count++;
    return count;
  });

  readonly enabledCount = computed(() =>
    this._rules().filter(r => r.isEnabled).length,
  );

  // ── Mutations ──
  setRules(rules: RoutingRule[], total: number): void {
    this._rules.set(rules);
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

  updateFilter(partial: Partial<RoutingRuleFilter>): void {
    this._filter.update(f => ({ ...f, ...partial }));
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  clearFilters(): void {
    this._filter.set({});
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  removeRuleFromList(id: string): void {
    this._rules.update(list => list.filter(r => r.id !== id));
  }
}
