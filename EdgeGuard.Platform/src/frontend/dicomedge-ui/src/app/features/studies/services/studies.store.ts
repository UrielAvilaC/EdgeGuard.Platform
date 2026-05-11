import { computed, Injectable, signal } from '@angular/core';

import { PaginationMeta } from '../../../shared/models/pagination.model';
import { SortParams } from '../../../shared/models/sort.model';
import { Study, StudyFilter } from '../models/study.models';

@Injectable()
export class StudiesStore {
  // ── Private state ──
  private readonly _studies = signal<Study[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _pagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _sort = signal<SortParams>({ sortBy: 'createdAt', sortDir: 'desc' });
  private readonly _filter = signal<StudyFilter>({});
  private readonly _selectedStudy = signal<Study | null>(null);
  private readonly _selectedLoading = signal(false);

  // ── Public readonly ──
  readonly studies = this._studies.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly pagination = this._pagination.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly filter = this._filter.asReadonly();
  readonly selectedStudy = this._selectedStudy.asReadonly();
  readonly selectedLoading = this._selectedLoading.asReadonly();

  // ── Computed ──
  readonly hasData = computed(() => this._studies().length > 0);
  readonly totalPages = computed(() => {
    const p = this._pagination();
    return Math.ceil(p.total / p.pageSize) || 1;
  });

  readonly activeFilterCount = computed(() => {
    const f = this._filter();
    let count = 0;
    if (f.search) count++;
    if (f.status) count++;
    if (f.sourceNodeId) count++;
    if (f.modality) count++;
    if (f.dateFrom || f.dateTo) count++;
    if (f.isUrgent !== undefined) count++;
    return count;
  });

  // ── Mutations ──
  setStudies(studies: Study[], total: number): void {
    this._studies.set(studies);
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

  setFilter(filter: StudyFilter): void {
    this._filter.set(filter);
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  updateFilter(partial: Partial<StudyFilter>): void {
    this._filter.update(f => ({ ...f, ...partial }));
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  clearFilters(): void {
    this._filter.set({});
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  setSelectedStudy(study: Study | null): void {
    this._selectedStudy.set(study);
  }

  setSelectedLoading(loading: boolean): void {
    this._selectedLoading.set(loading);
  }

  updateStudyInList(updated: Study): void {
    this._studies.update(list =>
      list.map(s => (s.id === updated.id ? updated : s)),
    );
    if (this._selectedStudy()?.id === updated.id) {
      this._selectedStudy.set(updated);
    }
  }
}
