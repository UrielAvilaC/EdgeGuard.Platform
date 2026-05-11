import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SortParams } from '../../../shared/models/sort.model';
import { RoutingRulesApiService } from '../infrastructure/routing-rules-api.service';
import {
  CreateRoutingRuleRequest,
  RoutingRuleFilter,
  UpdateRoutingRuleRequest,
} from '../models/routing-rule.models';
import { RoutingRulesStore } from './routing-rules.store';

@Injectable()
export class RoutingRulesFacade {
  private readonly api = inject(RoutingRulesApiService);
  private readonly store = inject(RoutingRulesStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly rules = this.store.rules;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly pagination = this.store.pagination;
  readonly sort = this.store.sort;
  readonly filter = this.store.filter;
  readonly hasData = this.store.hasData;
  readonly activeFilterCount = this.store.activeFilterCount;
  readonly enabledCount = this.store.enabledCount;

  // ── Actions ──
  loadRules(): void {
    const filter = this.store.filter();
    const pagination = this.store.pagination();
    const sort = this.store.sort();

    const params: RoutingRuleFilter = {
      ...filter,
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sort.sortBy,
      sortDir: sort.sortDir,
    };

    this.store.setLoading(true);
    this.api.getRules(params).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => this.store.setRules(result.items, result.total),
      error: () => {
        this.store.setError('Error al cargar reglas de ruteo');
        this.toast.error('No se pudieron cargar las reglas de ruteo');
      },
    });
  }

  changePage(page: number, pageSize: number): void {
    this.store.setPagination(page, pageSize);
    this.loadRules();
  }

  changeSort(sort: SortParams): void {
    this.store.setSort(sort);
    this.loadRules();
  }

  updateFilter(partial: Partial<RoutingRuleFilter>): void {
    this.store.updateFilter(partial);
    this.loadRules();
  }

  clearFilters(): void {
    this.store.clearFilters();
    this.loadRules();
  }

  createRule(request: CreateRoutingRuleRequest): void {
    this.store.setLoading(true);
    this.api.create(request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla de ruteo creada correctamente');
        this.loadRules();
      },
      error: () => this.toast.error('No se pudo crear la regla de ruteo'),
    });
  }

  updateRule(id: string, request: UpdateRoutingRuleRequest): void {
    this.store.setLoading(true);
    this.api.update(id, request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla de ruteo actualizada');
        this.loadRules();
      },
      error: () => this.toast.error('No se pudo actualizar la regla de ruteo'),
    });
  }

  enableRule(id: string): void {
    this.api.enable(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla habilitada');
        this.loadRules();
      },
      error: () => this.toast.error('No se pudo habilitar la regla'),
    });
  }

  disableRule(id: string): void {
    this.api.disable(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla deshabilitada');
        this.loadRules();
      },
      error: () => this.toast.error('No se pudo deshabilitar la regla'),
    });
  }

  updatePriority(id: string, priority: number): void {
    this.api.updatePriority(id, { priority }).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Prioridad actualizada');
        this.loadRules();
      },
      error: () => this.toast.error('No se pudo actualizar la prioridad'),
    });
  }

  deleteRule(id: string): void {
    this.store.setLoading(true);
    this.api.delete(id).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.store.removeRuleFromList(id);
        this.toast.success('Regla de ruteo eliminada');
      },
      error: () => this.toast.error('No se pudo eliminar la regla de ruteo'),
    });
  }
}
