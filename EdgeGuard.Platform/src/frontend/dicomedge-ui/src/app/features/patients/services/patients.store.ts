import { computed, Injectable, signal } from '@angular/core';

import { PaginationMeta } from '../../../shared/models/pagination.model';
import { SortParams } from '../../../shared/models/sort.model';
import { Patient, PatientFilter } from '../models/patient.models';

@Injectable()
export class PatientsStore {
  // ── Private state ──
  private readonly _patients = signal<Patient[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _pagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _sort = signal<SortParams>({ sortBy: 'createdAt', sortDir: 'desc' });
  private readonly _filter = signal<PatientFilter>({});
  private readonly _selectedPatient = signal<Patient | null>(null);
  private readonly _selectedLoading = signal(false);

  // ── Public readonly ──
  readonly patients = this._patients.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly pagination = this._pagination.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly filter = this._filter.asReadonly();
  readonly selectedPatient = this._selectedPatient.asReadonly();
  readonly selectedLoading = this._selectedLoading.asReadonly();

  // ── Computed ──
  readonly hasData = computed(() => this._patients().length > 0);

  readonly activeFilterCount = computed(() => {
    const f = this._filter();
    let count = 0;
    if (f.search) count++;
    if (f.createdByNodeId) count++;
    if (f.isActive !== undefined) count++;
    if (f.hasPhone !== undefined) count++;
    if (f.hasEmail !== undefined) count++;
    return count;
  });

  // ── Mutations ──
  setPatients(patients: Patient[], total: number): void {
    this._patients.set(patients);
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

  setFilter(filter: PatientFilter): void {
    this._filter.set(filter);
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  updateFilter(partial: Partial<PatientFilter>): void {
    this._filter.update(f => ({ ...f, ...partial }));
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  clearFilters(): void {
    this._filter.set({});
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  setSelectedPatient(patient: Patient | null): void {
    this._selectedPatient.set(patient);
  }

  setSelectedLoading(loading: boolean): void {
    this._selectedLoading.set(loading);
  }

  updatePatientInList(updated: Patient): void {
    this._patients.update(list =>
      list.map(p => (p.id === updated.id ? updated : p)),
    );
    if (this._selectedPatient()?.id === updated.id) {
      this._selectedPatient.set(updated);
    }
  }
}
