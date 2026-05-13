import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SortParams } from '../../../shared/models/sort.model';
import { AuditApiService } from '../infrastructure/audit-api.service';
import { AuditLogFilter } from '../models/audit.models';
import { AuditStore } from './audit.store';

@Injectable()
export class AuditFacade {
  private readonly api = inject(AuditApiService);
  private readonly store = inject(AuditStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly logs = this.store.logs;
  readonly selectedLog = this.store.selectedLog;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly pagination = this.store.pagination;
  readonly sort = this.store.sort;
  readonly filter = this.store.filter;
  readonly hasData = this.store.hasData;
  readonly activeFilterCount = this.store.activeFilterCount;
  readonly eventTypes = this.store.eventTypes;

  loadLogs(): void {
    const filter = this.store.filter();
    const pagination = this.store.pagination();
    const sort = this.store.sort();

    const params: AuditLogFilter = {
      ...filter,
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sort.sortBy,
      sortDir: sort.sortDir,
    };

    this.store.setLoading(true);
    this.api.getPaged(params).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: result => this.store.setLogs(result.items, result.page, result.pageSize, result.total),
      error: () => {
        this.store.setError('Error al cargar registros de auditoría');
        this.toast.error('No se pudieron cargar los registros');
      },
    });
  }

  loadEventTypes(): void {
    this.api.getEventTypes().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: types => this.store.setEventTypes(types),
    });
  }

  changePage(page: number, pageSize: number): void {
    this.store.setPagination(page, pageSize);
    this.loadLogs();
  }

  changeSort(sort: SortParams): void {
    this.store.setSort(sort);
    this.loadLogs();
  }

  updateFilter(partial: Partial<AuditLogFilter>): void {
    this.store.updateFilter(partial);
    this.loadLogs();
  }

  clearFilters(): void {
    this.store.clearFilters();
    this.loadLogs();
  }

  selectLog(log: typeof this.store.selectedLog extends () => infer T ? T : never): void {
    this.store.setSelectedLog(log);
  }
}
