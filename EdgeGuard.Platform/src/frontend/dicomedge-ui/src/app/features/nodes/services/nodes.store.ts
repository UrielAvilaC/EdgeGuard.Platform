import { computed, Injectable, signal } from '@angular/core';

import { PaginationMeta } from '../../../shared/models/pagination.model';
import { SortParams } from '../../../shared/models/sort.model';
import { Node, NodeFilter } from '../models/node.models';

@Injectable()
export class NodesStore {
  // ── Private state ──
  private readonly _nodes = signal<Node[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _pagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _sort = signal<SortParams>({ sortBy: 'name', sortDir: 'asc' });
  private readonly _filter = signal<NodeFilter>({});
  private readonly _selectedNode = signal<Node | null>(null);
  private readonly _selectedLoading = signal(false);

  // ── Public readonly ──
  readonly nodes = this._nodes.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly pagination = this._pagination.asReadonly();
  readonly sort = this._sort.asReadonly();
  readonly filter = this._filter.asReadonly();
  readonly selectedNode = this._selectedNode.asReadonly();
  readonly selectedLoading = this._selectedLoading.asReadonly();

  // ── Computed ──
  readonly hasData = computed(() => this._nodes().length > 0);

  readonly activeFilterCount = computed(() => {
    const f = this._filter();
    let count = 0;
    if (f.search) count++;
    if (f.status) count++;
    if (f.isEnabled !== undefined) count++;
    return count;
  });

  readonly onlineCount = computed(() =>
    this._nodes().filter(n => n.status === 'Online').length,
  );

  // ── Mutations ──
  setNodes(nodes: Node[], total: number): void {
    this._nodes.set(nodes);
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

  setFilter(filter: NodeFilter): void {
    this._filter.set(filter);
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  updateFilter(partial: Partial<NodeFilter>): void {
    this._filter.update(f => ({ ...f, ...partial }));
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  clearFilters(): void {
    this._filter.set({});
    this._pagination.update(p => ({ ...p, page: 1 }));
  }

  setSelectedNode(node: Node | null): void {
    this._selectedNode.set(node);
  }

  setSelectedLoading(loading: boolean): void {
    this._selectedLoading.set(loading);
  }

  updateNodeInList(updated: Node): void {
    this._nodes.update(list =>
      list.map(n => (n.id === updated.id ? updated : n)),
    );
    if (this._selectedNode()?.id === updated.id) {
      this._selectedNode.set(updated);
    }
  }

  removeNodeFromList(id: string): void {
    this._nodes.update(list => list.filter(n => n.id !== id));
  }
}
