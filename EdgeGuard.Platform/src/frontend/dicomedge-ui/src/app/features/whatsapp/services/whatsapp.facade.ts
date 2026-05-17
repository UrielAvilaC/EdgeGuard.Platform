import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';
import { WhatsAppApiService } from '../infrastructure/whatsapp-api.service';
import {
  CreateWhatsAppAutoSendRuleRequest,
  CreateWhatsAppTemplateRequest,
  UpdateWhatsAppAutoSendRuleRequest,
  UpdateWhatsAppTemplateRequest,
} from '../models/whatsapp.models';
import { WhatsAppStore } from './whatsapp.store';

@Injectable()
export class WhatsAppFacade {
  private readonly api = inject(WhatsAppApiService);
  private readonly store = inject(WhatsAppStore);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose store state ──
  readonly configStatus = this.store.configStatus;
  readonly tags = this.store.tags;
  readonly templates = this.store.templates;
  readonly autoSendRules = this.store.autoSendRules;
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly activeTab = this.store.activeTab;
  readonly activeTemplatesCount = this.store.activeTemplatesCount;
  readonly enabledRulesCount = this.store.enabledRulesCount;
  readonly tagOptions = this.store.tagOptions;

  // ── Tab ──
  setActiveTab(tab: 'templates' | 'rules'): void {
    this.store.setActiveTab(tab);
  }

  // ── Config ──
  loadConfigStatus(): void {
    this.api.getConfigStatus().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (status) => this.store.setConfigStatus(status),
      error: () => this.toast.error('No se pudo cargar la configuración de WhatsApp'),
    });
  }

  loadTags(): void {
    this.api.getTags().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (tags) => this.store.setTags(tags),
      error: () => this.toast.error('No se pudieron cargar los tags'),
    });
  }

  // ── Templates ──
  loadTemplates(): void {
    this.store.setLoading(true);
    this.api.getTemplates().pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (templates) => this.store.setTemplates(templates),
      error: () => {
        this.store.setError('Error al cargar templates');
        this.toast.error('No se pudieron cargar los templates');
      },
    });
  }

  createTemplate(request: CreateWhatsAppTemplateRequest): void {
    this.store.setLoading(true);
    this.api.createTemplate(request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Template creado correctamente');
        this.loadTemplates();
        this.loadConfigStatus();
      },
      error: () => this.toast.error('No se pudo crear el template'),
    });
  }

  updateTemplate(id: string, request: UpdateWhatsAppTemplateRequest): void {
    this.api.updateTemplate(id, request).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Template actualizado');
        this.loadTemplates();
      },
      error: () => this.toast.error('No se pudo actualizar el template'),
    });
  }

  deleteTemplate(id: string): void {
    this.api.deleteTemplate(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Template eliminado');
        this.store.removeTemplate(id);
        this.loadConfigStatus();
      },
      error: () => this.toast.error('No se pudo eliminar el template'),
    });
  }

  // ── Auto-Send Rules ──
  loadAutoSendRules(): void {
    this.store.setLoading(true);
    this.api.getAutoSendRules().pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (rules) => this.store.setAutoSendRules(rules),
      error: () => {
        this.store.setError('Error al cargar reglas auto-send');
        this.toast.error('No se pudieron cargar las reglas');
      },
    });
  }

  createAutoSendRule(request: CreateWhatsAppAutoSendRuleRequest): void {
    this.store.setLoading(true);
    this.api.createAutoSendRule(request).pipe(
      finalize(() => this.store.setLoading(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla creada correctamente');
        this.loadAutoSendRules();
        this.loadConfigStatus();
      },
      error: () => this.toast.error('No se pudo crear la regla'),
    });
  }

  updateAutoSendRule(id: string, request: UpdateWhatsAppAutoSendRuleRequest): void {
    this.api.updateAutoSendRule(id, request).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (updated) => {
        this.toast.success('Regla actualizada');
        this.store.updateRuleInList(updated);
      },
      error: () => this.toast.error('No se pudo actualizar la regla'),
    });
  }

  deleteAutoSendRule(id: string): void {
    this.api.deleteAutoSendRule(id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Regla eliminada');
        this.store.removeRule(id);
        this.loadConfigStatus();
      },
      error: () => this.toast.error('No se pudo eliminar la regla'),
    });
  }
}
