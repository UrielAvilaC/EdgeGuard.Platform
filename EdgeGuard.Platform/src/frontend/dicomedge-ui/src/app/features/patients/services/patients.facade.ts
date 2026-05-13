import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SortParams } from '../../../shared/models/sort.model';
import { PatientsApiService } from '../infrastructure/patients-api.service';
import { PatientFilter, UpdatePatientRequest, ImportResult } from '../models/patient.models';
import { PatientsStore } from './patients.store';

@Injectable()
export class PatientsFacade {
  private readonly api = inject(PatientsApiService);
  private readonly store = inject(PatientsStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly patients = this.store.patients;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly pagination = this.store.pagination;
  readonly sort = this.store.sort;
  readonly filter = this.store.filter;
  readonly selectedPatient = this.store.selectedPatient;
  readonly selectedLoading = this.store.selectedLoading;
  readonly hasData = this.store.hasData;
  readonly activeFilterCount = this.store.activeFilterCount;

  // ── Actions ──
  loadPatients(): void {
    const filter = this.store.filter();
    const pagination = this.store.pagination();
    const sort = this.store.sort();

    const params: PatientFilter = {
      ...filter,
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sort.sortBy,
      sortDir: sort.sortDir,
    };

    this.store.setLoading(true);
    this.api.getPatients(params).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => this.store.setPatients(result.items, result.page, result.pageSize, result.total),
      error: () => {
        this.store.setError('Error al cargar pacientes');
        this.toast.error('No se pudieron cargar los pacientes');
      },
    });
  }

  changePage(page: number, pageSize: number): void {
    this.store.setPagination(page, pageSize);
    this.loadPatients();
  }

  changeSort(sort: SortParams): void {
    this.store.setSort(sort);
    this.loadPatients();
  }

  updateFilter(partial: Partial<PatientFilter>): void {
    this.store.updateFilter(partial);
    this.loadPatients();
  }

  clearFilters(): void {
    this.store.clearFilters();
    this.loadPatients();
  }

  loadPatientById(id: string): void {
    this.store.setSelectedLoading(true);
    this.api.getById(id).pipe(
      finalize(() => this.store.setSelectedLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (patient) => this.store.setSelectedPatient(patient),
      error: () => {
        this.store.setError('Error al cargar el paciente');
        this.toast.error('No se pudo cargar el detalle del paciente');
      },
    });
  }

  updatePatient(id: string, request: UpdatePatientRequest): void {
    this.store.setSelectedLoading(true);
    this.api.update(id, request).pipe(
      finalize(() => this.store.setSelectedLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (updated) => {
        this.store.updatePatientInList(updated);
        this.toast.success('Paciente actualizado correctamente');
      },
      error: () => this.toast.error('No se pudo actualizar el paciente'),
    });
  }

  exportCsv(): void {
    const filter = this.store.filter();
    const sort = this.store.sort();
    const params: PatientFilter = { ...filter, sortBy: sort.sortBy, sortDir: sort.sortDir };

    this.api.export(params).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) return;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = this.getExportFileName(response.headers);
        a.click();
        URL.revokeObjectURL(url);
        this.toast.success('Exportación completada');
      },
      error: () => this.toast.error('Error al exportar pacientes'),
    });
  }

  importCsv(file: File): void {
    this.store.setLoading(true);
    this.api.import(file).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result: ImportResult) => {
        this.toast.success(`Importación completada: ${result.successCount} de ${result.totalRecords} registros`);
        if (result.errorCount > 0) {
          this.toast.warning(`${result.errorCount} registros con errores`);
        }
        this.loadPatients();
      },
      error: () => this.toast.error('Error al importar pacientes'),
    });
  }

  private getExportFileName(headers: import('@angular/common/http').HttpHeaders): string {
    const disposition = headers.get('content-disposition');
    if (disposition) {
      const match = disposition.match(/filename="?([^";\s]+)"?/);
      if (match) return match[1];
    }
    return `pacientes_${new Date().toISOString().slice(0, 10)}.csv`;
  }
}
