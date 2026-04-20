import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { SortParams } from '../../../shared/models/sort.model';
import { Hl7StatusApiService } from '../infrastructure/hl7-status-api.service';
import { QueueApiService } from '../infrastructure/queue-api.service';
import { RoutingRulesApiService } from '../infrastructure/routing-rules-api.service';
import {
  CreateRoutingRuleRequest,
  Hl7DispatchStatus,
  RoutingRuleFilter,
  UpdatePriorityRequest,
  UpdateRoutingRuleRequest,
} from '../models/hl7.models';
import { Hl7Store } from './hl7.store';

@Injectable()
export class Hl7Facade {
  private readonly statusApi = inject(Hl7StatusApiService);
  private readonly queueApi = inject(QueueApiService);
  private readonly rulesApi = inject(RoutingRulesApiService);
  private readonly store = inject(Hl7Store);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly listenerStatus = this.store.listenerStatus;
  readonly recentMessages = this.store.recentMessages;
  readonly selectedMessage = this.store.selectedMessage;
  readonly queueSummary = this.store.queueSummary;
  readonly queuedMessages = this.store.queuedMessages;
  readonly messagesByStatus = this.store.messagesByStatus;
  readonly selectedDispatchStatus = this.store.selectedDispatchStatus;
  readonly rules = this.store.rules;
  readonly rulesPagination = this.store.rulesPagination;
  readonly rulesSort = this.store.rulesSort;
  readonly rulesFilter = this.store.rulesFilter;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly activeTab = this.store.activeTab;
  readonly hasRules = this.store.hasRules;
  readonly activeRulesFilterCount = this.store.activeRulesFilterCount;
  readonly enabledRulesCount = this.store.enabledRulesCount;

  // ── Tab ──
  setActiveTab(tab: 'listener' | 'routing' | 'queue'): void {
    this.store.setActiveTab(tab);
  }

  // ── Listener ──
  loadListenerStatus(): void {
    this.store.setLoading(true);
    this.statusApi.getStatus().pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (status) => this.store.setListenerStatus(status),
      error: () => {
        this.store.setError('Error al cargar estado del listener');
        this.toast.error('No se pudo cargar el estado HL7');
      },
    });
  }

  loadRecentMessages(count = 20): void {
    this.statusApi.getRecentMessages(count).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (messages) => this.store.setRecentMessages(messages),
      error: () => this.toast.error('No se pudieron cargar los mensajes recientes'),
    });
  }

  loadMessageDetail(id: string): void {
    this.statusApi.getMessageDetail(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (detail) => this.store.setSelectedMessage(detail),
      error: () => this.toast.error('No se pudo cargar el detalle del mensaje'),
    });
  }

  clearSelectedMessage(): void {
    this.store.setSelectedMessage(null);
  }

  // ── Queue ──
  loadQueueSummary(): void {
    this.store.setLoading(true);
    this.queueApi.getSummary().pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (summary) => this.store.setQueueSummary(summary),
      error: () => {
        this.store.setError('Error al cargar resumen de cola');
        this.toast.error('No se pudo cargar el resumen de la cola');
      },
    });
  }

  loadQueuedMessages(batchSize = 50): void {
    this.queueApi.getQueued(batchSize).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (messages) => this.store.setQueuedMessages(messages),
      error: () => this.toast.error('No se pudieron cargar los mensajes encolados'),
    });
  }

  loadMessagesByDispatchStatus(status: Hl7DispatchStatus): void {
    this.store.setSelectedDispatchStatus(status);
    this.store.setLoading(true);
    this.queueApi.getByDispatchStatus(status).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (messages) => this.store.setMessagesByStatus(messages),
      error: () => this.toast.error('No se pudieron cargar los mensajes'),
    });
  }

  // ── Routing Rules ──
  loadRules(): void {
    const filter = this.store.rulesFilter();
    const pagination = this.store.rulesPagination();
    const sort = this.store.rulesSort();

    const params: RoutingRuleFilter = {
      ...filter,
      page: pagination.page,
      pageSize: pagination.pageSize,
      sortBy: sort.sortBy,
      sortDir: sort.sortDir,
    };

    this.store.setLoading(true);
    this.rulesApi.getRules(params).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => this.store.setRules(result.items, result.total),
      error: () => {
        this.store.setError('Error al cargar reglas de enrutamiento');
        this.toast.error('No se pudieron cargar las reglas');
      },
    });
  }

  changeRulesPage(page: number, pageSize: number): void {
    this.store.setRulesPagination(page, pageSize);
    this.loadRules();
  }

  changeRulesSort(sort: SortParams): void {
    this.store.setRulesSort(sort);
    this.loadRules();
  }

  updateRulesFilter(partial: Partial<RoutingRuleFilter>): void {
    this.store.updateRulesFilter(partial);
    this.loadRules();
  }

  clearRulesFilters(): void {
    this.store.clearRulesFilters();
    this.loadRules();
  }

  createRule(request: CreateRoutingRuleRequest): void {
    this.store.setLoading(true);
    this.rulesApi.create(request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla creada correctamente');
        this.loadRules();
      },
      error: () => this.toast.error('No se pudo crear la regla'),
    });
  }

  updateRule(id: string, request: UpdateRoutingRuleRequest): void {
    this.rulesApi.update(id, request).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla actualizada');
        this.loadRules();
      },
      error: () => this.toast.error('No se pudo actualizar la regla'),
    });
  }

  enableRule(id: string): void {
    this.rulesApi.enable(id).pipe(
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
    this.rulesApi.disable(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla deshabilitada');
        this.loadRules();
      },
      error: () => this.toast.error('No se pudo deshabilitar la regla'),
    });
  }

  updateRulePriority(id: string, priority: number): void {
    const request: UpdatePriorityRequest = { priority };
    this.rulesApi.updatePriority(id, request).pipe(
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
    this.rulesApi.delete(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla eliminada');
        this.store.removeRuleFromList(id);
      },
      error: () => this.toast.error('No se pudo eliminar la regla'),
    });
  }
}
