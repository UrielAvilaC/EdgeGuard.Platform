import { computed, Injectable, signal } from '@angular/core';

import { PaginationMeta } from '../../../shared/models/pagination.model';
import { SortParams } from '../../../shared/models/sort.model';
import {
  Hl7ListenerStatus,
  Hl7MessageSummary,
  Hl7MessageDetail,
  QueueSummary,
  Hl7MessageQueued,
  RoutingRule,
  RoutingRuleFilter,
  Hl7DispatchStatus,
  Hl7Message,
} from '../models/hl7.models';

@Injectable()
export class Hl7Store {
  // ── Listener ──
  private readonly _listenerStatus = signal<Hl7ListenerStatus | null>(null);
  private readonly _recentMessages = signal<Hl7MessageSummary[]>([]);
  private readonly _selectedMessage = signal<Hl7MessageDetail | null>(null);

  // ── Queue ──
  private readonly _queueSummary = signal<QueueSummary | null>(null);
  private readonly _queuedMessages = signal<Hl7MessageQueued[]>([]);
  private readonly _messagesByStatus = signal<Hl7Message[]>([]);
  private readonly _selectedDispatchStatus = signal<Hl7DispatchStatus | null>(null);

  // ── Routing Rules ──
  private readonly _rules = signal<RoutingRule[]>([]);
  private readonly _rulesPagination = signal<PaginationMeta>({ page: 1, pageSize: 25, total: 0 });
  private readonly _rulesSort = signal<SortParams>({ sortBy: 'priority', sortDir: 'asc' });
  private readonly _rulesFilter = signal<RoutingRuleFilter>({});

  // ── Common ──
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _activeTab = signal<'listener' | 'routing' | 'queue'>('listener');

  // ── Public readonly ──
  readonly listenerStatus = this._listenerStatus.asReadonly();
  readonly recentMessages = this._recentMessages.asReadonly();
  readonly selectedMessage = this._selectedMessage.asReadonly();
  readonly queueSummary = this._queueSummary.asReadonly();
  readonly queuedMessages = this._queuedMessages.asReadonly();
  readonly messagesByStatus = this._messagesByStatus.asReadonly();
  readonly selectedDispatchStatus = this._selectedDispatchStatus.asReadonly();
  readonly rules = this._rules.asReadonly();
  readonly rulesPagination = this._rulesPagination.asReadonly();
  readonly rulesSort = this._rulesSort.asReadonly();
  readonly rulesFilter = this._rulesFilter.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly activeTab = this._activeTab.asReadonly();

  // ── Computed ──
  readonly hasRules = computed(() => this._rules().length > 0);

  readonly activeRulesFilterCount = computed(() => {
    const f = this._rulesFilter();
    let count = 0;
    if (f.search) count++;
    if (f.isEnabled !== undefined) count++;
    if (f.targetNodeId) count++;
    return count;
  });

  readonly enabledRulesCount = computed(() =>
    this._rules().filter(r => r.isEnabled).length,
  );

  // ── Mutations: Listener ──
  setListenerStatus(status: Hl7ListenerStatus): void {
    this._listenerStatus.set(status);
  }

  setRecentMessages(messages: Hl7MessageSummary[]): void {
    this._recentMessages.set(messages);
  }

  setSelectedMessage(message: Hl7MessageDetail | null): void {
    this._selectedMessage.set(message);
  }

  // ── Mutations: Queue ──
  setQueueSummary(summary: QueueSummary): void {
    this._queueSummary.set(summary);
  }

  setQueuedMessages(messages: Hl7MessageQueued[]): void {
    this._queuedMessages.set(messages);
  }

  setMessagesByStatus(messages: Hl7Message[]): void {
    this._messagesByStatus.set(messages);
  }

  setSelectedDispatchStatus(status: Hl7DispatchStatus | null): void {
    this._selectedDispatchStatus.set(status);
  }

  // ── Mutations: Rules ──
  setRules(rules: RoutingRule[], total: number): void {
    this._rules.set(rules);
    this._rulesPagination.update(p => ({ ...p, total }));
    this._error.set(null);
  }

  setRulesPagination(page: number, pageSize: number): void {
    this._rulesPagination.update(p => ({ ...p, page, pageSize }));
  }

  setRulesSort(sort: SortParams): void {
    this._rulesSort.set(sort);
  }

  setRulesFilter(filter: RoutingRuleFilter): void {
    this._rulesFilter.set(filter);
    this._rulesPagination.update(p => ({ ...p, page: 1 }));
  }

  updateRulesFilter(partial: Partial<RoutingRuleFilter>): void {
    this._rulesFilter.update(f => ({ ...f, ...partial }));
    this._rulesPagination.update(p => ({ ...p, page: 1 }));
  }

  clearRulesFilters(): void {
    this._rulesFilter.set({});
    this._rulesPagination.update(p => ({ ...p, page: 1 }));
  }

  updateRuleInList(updated: RoutingRule): void {
    this._rules.update(list =>
      list.map(r => (r.id === updated.id ? updated : r)),
    );
  }

  removeRuleFromList(id: string): void {
    this._rules.update(list => list.filter(r => r.id !== id));
  }

  // ── Common ──
  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setError(error: string | null): void {
    this._error.set(error);
  }

  setActiveTab(tab: 'listener' | 'routing' | 'queue'): void {
    this._activeTab.set(tab);
  }
}
