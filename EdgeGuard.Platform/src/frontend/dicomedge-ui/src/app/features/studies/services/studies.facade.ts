import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SortParams } from '../../../shared/models/sort.model';
import { StudiesApiService } from '../infrastructure/studies-api.service';
import { StudyFilter, UpdateStudyRequest, UpdateStudyStatusRequest } from '../models/study.models';
import { StudiesStore } from './studies.store';

@Injectable()
export class StudiesFacade {
  private readonly api = inject(StudiesApiService);
  private readonly store = inject(StudiesStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly studies = this.store.studies;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly pagination = this.store.pagination;
  readonly sort = this.store.sort;
  readonly filter = this.store.filter;
  readonly selectedStudy = this.store.selectedStudy;
  readonly selectedLoading = this.store.selectedLoading;
  readonly hasData = this.store.hasData;
  readonly totalPages = this.store.totalPages;
  readonly activeFilterCount = this.store.activeFilterCount;

  // ── Actions ──
  loadStudies(): void {
    const filter = this.store.filter();
    const pagination = this.store.pagination();
    const sort = this.store.sort();

    const params: StudyFilter = {
      ...filter,
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sort.sortBy,
      sortDir: sort.sortDir,
    };

    this.store.setLoading(true);
    this.api.getStudies(params).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => this.store.setStudies(result.items, result.page, result.pageSize, result.total),
      error: () => {
        this.store.setError('Error al cargar estudios');
        this.toast.error('No se pudieron cargar los estudios');
      },
    });
  }

  changePage(page: number, pageSize: number): void {
    this.store.setPagination(page, pageSize);
    this.loadStudies();
  }

  changeSort(sort: SortParams): void {
    this.store.setSort(sort);
    this.loadStudies();
  }

  updateFilter(partial: Partial<StudyFilter>): void {
    this.store.updateFilter(partial);
    this.loadStudies();
  }

  clearFilters(): void {
    this.store.clearFilters();
    this.loadStudies();
  }

  loadStudyById(id: string): void {
    this.store.setSelectedLoading(true);
    this.api.getById(id).pipe(
      finalize(() => this.store.setSelectedLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (study) => this.store.setSelectedStudy(study),
      error: () => {
        this.store.setError('Error al cargar el estudio');
        this.toast.error('No se pudo cargar el detalle del estudio');
      },
    });
  }

  updateStudy(id: string, request: UpdateStudyRequest): void {
    this.store.setSelectedLoading(true);
    this.api.update(id, request).pipe(
      finalize(() => this.store.setSelectedLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (updated) => {
        this.store.updateStudyInList(updated);
        this.toast.success('Estudio actualizado correctamente');
      },
      error: () => this.toast.error('No se pudo actualizar el estudio'),
    });
  }

  updateStatus(id: string, request: UpdateStudyStatusRequest): void {
    this.store.setSelectedLoading(true);
    this.api.updateStatus(id, request).pipe(
      finalize(() => this.store.setSelectedLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (updated) => {
        this.store.updateStudyInList(updated);
        this.toast.success('Estado del estudio actualizado');
      },
      error: () => this.toast.error('No se pudo cambiar el estado del estudio'),
    });
  }

  exportCsv(): void {
    const filter = this.store.filter();
    const sort = this.store.sort();
    const params: StudyFilter = { ...filter, sortBy: sort.sortBy, sortDir: sort.sortDir };

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
      error: () => this.toast.error('Error al exportar estudios'),
    });
  }

  private getExportFileName(headers: import('@angular/common/http').HttpHeaders): string {
    const disposition = headers.get('content-disposition');
    if (disposition) {
      const match = disposition.match(/filename="?([^";\s]+)"?/);
      if (match) return match[1];
    }
    return `estudios_${new Date().toISOString().slice(0, 10)}.csv`;
  }
}
