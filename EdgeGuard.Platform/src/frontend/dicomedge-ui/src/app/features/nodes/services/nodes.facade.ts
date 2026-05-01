import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SortParams } from '../../../shared/models/sort.model';
import { NodesApiService } from '../infrastructure/nodes-api.service';
import { CreateNodeRequest, NodeFilter, UpdateNodeRequest } from '../models/node.models';
import { NodesStore } from './nodes.store';

@Injectable()
export class NodesFacade {
  private readonly api = inject(NodesApiService);
  private readonly store = inject(NodesStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly nodes = this.store.nodes;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly pagination = this.store.pagination;
  readonly sort = this.store.sort;
  readonly filter = this.store.filter;
  readonly selectedNode = this.store.selectedNode;
  readonly selectedLoading = this.store.selectedLoading;
  readonly hasData = this.store.hasData;
  readonly activeFilterCount = this.store.activeFilterCount;
  readonly onlineCount = this.store.onlineCount;

  // ── Actions ──
  loadNodes(): void {
    const filter = this.store.filter();
    const pagination = this.store.pagination();
    const sort = this.store.sort();

    const params: NodeFilter = {
      ...filter,
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sort.sortBy,
      sortDir: sort.sortDir,
    };

    this.store.setLoading(true);
    this.api.getNodes(params).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => this.store.setNodes(result.items, result.total),
      error: () => {
        this.store.setError('Error al cargar nodos');
        this.toast.error('No se pudieron cargar los nodos');
      },
    });
  }

  changePage(page: number, pageSize: number): void {
    this.store.setPagination(page, pageSize);
    this.loadNodes();
  }

  changeSort(sort: SortParams): void {
    this.store.setSort(sort);
    this.loadNodes();
  }

  updateFilter(partial: Partial<NodeFilter>): void {
    this.store.updateFilter(partial);
    this.loadNodes();
  }

  clearFilters(): void {
    this.store.clearFilters();
    this.loadNodes();
  }

  loadNodeById(id: string): void {
    this.store.setSelectedLoading(true);
    this.api.getById(id).pipe(
      finalize(() => this.store.setSelectedLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (node) => this.store.setSelectedNode(node),
      error: () => {
        this.store.setError('Error al cargar el nodo');
        this.toast.error('No se pudo cargar el detalle del nodo');
      },
    });
  }

  createNode(request: CreateNodeRequest): void {
    this.store.setLoading(true);
    this.api.create(request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Nodo creado correctamente');
        this.loadNodes();
      },
      error: () => this.toast.error('No se pudo crear el nodo'),
    });
  }

  updateNode(id: string, request: UpdateNodeRequest): void {
    this.store.setSelectedLoading(true);
    this.api.update(id, request).pipe(
      finalize(() => this.store.setSelectedLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Nodo actualizado correctamente');
        this.loadNodeById(id);
      },
      error: () => this.toast.error('No se pudo actualizar el nodo'),
    });
  }

  enableNode(id: string): void {
    this.api.enable(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Nodo habilitado');
        this.loadNodes();
      },
      error: () => this.toast.error('No se pudo habilitar el nodo'),
    });
  }

  disableNode(id: string): void {
    this.api.disable(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Nodo deshabilitado');
        this.loadNodes();
      },
      error: () => this.toast.error('No se pudo deshabilitar el nodo'),
    });
  }
}
