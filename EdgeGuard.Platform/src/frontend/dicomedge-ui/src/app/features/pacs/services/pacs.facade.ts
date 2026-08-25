import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SortParams } from '../../../shared/models/sort.model';
import { PacsApiService } from '../infrastructure/pacs-api.service';
import { CreatePacsServerRequest, PacsServerFilter, UpdatePacsServerRequest } from '../models/pacs.models';
import { PacsStore } from './pacs.store';

@Injectable()
export class PacsFacade {
  private readonly api = inject(PacsApiService);
  private readonly store = inject(PacsStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly servers = this.store.servers;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly pagination = this.store.pagination;
  readonly sort = this.store.sort;
  readonly filter = this.store.filter;
  readonly hasData = this.store.hasData;
  readonly activeFilterCount = this.store.activeFilterCount;

  // ── Actions ──
  loadServers(): void {
    const filter = this.store.filter();
    const pagination = this.store.pagination();
    const sort = this.store.sort();

    const params: PacsServerFilter = {
      ...filter,
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sort.sortBy,
      sortDir: sort.sortDir,
    };

    this.store.setLoading(true);
    this.api.getPacsServers(params).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => this.store.setServers(result.items, result.page, result.pageSize, result.total),
      error: () => {
        this.store.setError('Error al cargar servidores PACS');
        this.toast.error('No se pudieron cargar los servidores PACS');
      },
    });
  }

  changePage(page: number, pageSize: number): void {
    this.store.setPagination(page, pageSize);
    this.loadServers();
  }

  changeSort(sort: SortParams): void {
    this.store.setSort(sort);
    this.loadServers();
  }

  updateFilter(partial: Partial<PacsServerFilter>): void {
    this.store.updateFilter(partial);
    this.loadServers();
  }

  clearFilters(): void {
    this.store.clearFilters();
    this.loadServers();
  }

  createServer(request: CreatePacsServerRequest): void {
    this.store.setLoading(true);
    this.api.create(request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Servidor PACS creado correctamente');
        this.loadServers();
      },
      error: () => this.toast.error('No se pudo crear el servidor PACS'),
    });
  }

  updateServer(id: string, request: UpdatePacsServerRequest): void {
    this.store.setLoading(true);
    this.api.update(id, request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Servidor PACS actualizado');
        this.loadServers();
      },
      error: () => this.toast.error('No se pudo actualizar el servidor PACS'),
    });
  }

  enableServer(id: string): void {
    this.api.enable(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Servidor PACS habilitado');
        this.loadServers();
      },
      error: () => this.toast.error('No se pudo habilitar el servidor'),
    });
  }

  disableServer(id: string): void {
    this.api.disable(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Servidor PACS deshabilitado');
        this.loadServers();
      },
      error: () => this.toast.error('No se pudo deshabilitar el servidor'),
    });
  }

  deleteServer(id: string): void {
    this.store.setLoading(true);
    this.api.delete(id).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.store.removeServerFromList(id);
        this.toast.success('Servidor PACS eliminado');
      },
      error: () => this.toast.error('No se pudo eliminar el servidor PACS'),
    });
  }
}
