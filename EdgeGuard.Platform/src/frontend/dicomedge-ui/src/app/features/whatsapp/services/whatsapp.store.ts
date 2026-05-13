import { computed, Injectable, signal } from '@angular/core';

import {
  WhatsAppConfigStatus,
  WhatsAppTemplate,
  WhatsAppTemplateTag,
  WhatsAppAutoSendRule,
} from '../models/whatsapp.models';

@Injectable()
export class WhatsAppStore {
  // ── Private state ──
  private readonly _configStatus = signal<WhatsAppConfigStatus | null>(null);
  private readonly _tags = signal<WhatsAppTemplateTag[]>([]);
  private readonly _templates = signal<WhatsAppTemplate[]>([]);
  private readonly _autoSendRules = signal<WhatsAppAutoSendRule[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _activeTab = signal<'templates' | 'rules'>('templates');

  // ── Public readonly ──
  readonly configStatus = this._configStatus.asReadonly();
  readonly tags = this._tags.asReadonly();
  readonly templates = this._templates.asReadonly();
  readonly autoSendRules = this._autoSendRules.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly activeTab = this._activeTab.asReadonly();

  // ── Computed ──
  readonly activeTemplatesCount = computed(() =>
    this._templates().filter(t => t.isActive).length,
  );

  readonly enabledRulesCount = computed(() =>
    this._autoSendRules().filter(r => r.isEnabled).length,
  );

  readonly tagOptions = computed(() =>
    this._tags().map(t => ({ value: t.tag, label: `${t.tag} — ${t.description}` })),
  );

  // ── Mutations: Config ──
  setConfigStatus(status: WhatsAppConfigStatus): void {
    this._configStatus.set(status);
  }

  setTags(tags: WhatsAppTemplateTag[]): void {
    this._tags.set(tags);
  }

  // ── Mutations: Templates ──
  setTemplates(templates: WhatsAppTemplate[]): void {
    this._templates.set(templates);
    this._error.set(null);
  }

  removeTemplate(id: string): void {
    this._templates.update(list => list.filter(t => t.id !== id));
  }

  // ── Mutations: Rules ──
  setAutoSendRules(rules: WhatsAppAutoSendRule[]): void {
    this._autoSendRules.set(rules);
  }

  updateRuleInList(updated: WhatsAppAutoSendRule): void {
    this._autoSendRules.update(list =>
      list.map(r => (r.id === updated.id ? updated : r)),
    );
  }

  removeRule(id: string): void {
    this._autoSendRules.update(list => list.filter(r => r.id !== id));
  }

  // ── Common ──
  setLoading(loading: boolean): void {
    this._loading.set(loading);
  }

  setError(error: string | null): void {
    this._error.set(error);
  }

  setActiveTab(tab: 'templates' | 'rules'): void {
    this._activeTab.set(tab);
  }
}
