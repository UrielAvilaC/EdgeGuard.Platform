import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faSync,
  faPlus,
  faPen,
  faTrash,
  faToggleOn,
  faToggleOff,
  faFileAlt,
  faRobot,
  faCircle,
  faCheck,
  faXmark as faXmarkIcon,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import {
  WhatsAppTemplate,
  WhatsAppAutoSendRule,
  CreateWhatsAppTemplateRequest,
  UpdateWhatsAppTemplateRequest,
  CreateWhatsAppAutoSendRuleRequest,
  UpdateWhatsAppAutoSendRuleRequest,
  STUDY_STATUS_OPTIONS,
} from '../../models/whatsapp.models';
import { WhatsAppStore } from '../../services/whatsapp.store';
import { WhatsAppFacade } from '../../services/whatsapp.facade';
import { TemplateFormDialog, TemplateFormDialogData } from '../template-form-dialog/template-form-dialog.component';
import { RuleFormDialog, RuleFormDialogData } from '../rule-form-dialog/rule-form-dialog.component';

@Component({
  selector: 'app-whatsapp-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [WhatsAppStore, WhatsAppFacade],
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiIconButton,
    UiChip,
    UiAlert,
  ],
  templateUrl: './whatsapp-page.component.html',
  styleUrl: './whatsapp-page.component.scss'
})
export default class WhatsappPage {
  protected readonly facade = inject(WhatsAppFacade);
  private readonly dialog = inject(MatDialog);

  protected readonly faSync = faSync;
  protected readonly faPlus = faPlus;
  protected readonly faPen = faPen;
  protected readonly faTrash = faTrash;
  protected readonly faToggleOn = faToggleOn;
  protected readonly faToggleOff = faToggleOff;
  protected readonly faCircle = faCircle;

  protected readonly tabs = [
    { key: 'templates' as const, label: 'Templates', icon: faFileAlt },
    { key: 'rules' as const, label: 'Reglas Auto-Send', icon: faRobot },
  ];

  constructor() {
    this.facade.loadConfigStatus();
    this.facade.loadTags();
    this.facade.loadTemplates();
  }

  protected onTabChange(tab: 'templates' | 'rules'): void {
    this.facade.setActiveTab(tab);
    if (tab === 'templates') this.facade.loadTemplates();
    if (tab === 'rules') this.facade.loadAutoSendRules();
  }

  protected refreshAll(): void {
    this.facade.loadConfigStatus();
    if (this.facade.activeTab() === 'templates') this.facade.loadTemplates();
    if (this.facade.activeTab() === 'rules') this.facade.loadAutoSendRules();
  }

  protected getStudyStatusLabel(status: string): string {
    return STUDY_STATUS_OPTIONS.find(o => o.value === status)?.label ?? status;
  }

  // ── Templates ──
  protected openCreateTemplateDialog(): void {
    this.dialog.open(TemplateFormDialog, {
      data: { tags: this.facade.tags() } satisfies TemplateFormDialogData,
    }).afterClosed().subscribe((result: CreateWhatsAppTemplateRequest | null) => {
      if (result) this.facade.createTemplate(result);
    });
  }

  protected openEditTemplateDialog(template: WhatsAppTemplate): void {
    this.dialog.open(TemplateFormDialog, {
      data: { template, tags: this.facade.tags() } satisfies TemplateFormDialogData,
    }).afterClosed().subscribe((result: UpdateWhatsAppTemplateRequest | null) => {
      if (result) this.facade.updateTemplate(template.id, result);
    });
  }

  protected confirmDeleteTemplate(template: WhatsAppTemplate): void {
    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Eliminar template',
        message: `¿Eliminar el template "${template.name}"? Esta acción no se puede deshacer.`,
        confirmText: 'Eliminar',
        confirmColor: 'warn',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) this.facade.deleteTemplate(template.id);
    });
  }

  // ── Auto-Send Rules ──
  protected openCreateRuleDialog(): void {
    this.dialog.open(RuleFormDialog, {
      data: { templates: this.facade.templates() } satisfies RuleFormDialogData,
    }).afterClosed().subscribe((result: CreateWhatsAppAutoSendRuleRequest | null) => {
      if (result) this.facade.createAutoSendRule(result);
    });
  }

  protected openEditRuleDialog(rule: WhatsAppAutoSendRule): void {
    this.dialog.open(RuleFormDialog, {
      data: { rule, templates: this.facade.templates() } satisfies RuleFormDialogData,
    }).afterClosed().subscribe((result: UpdateWhatsAppAutoSendRuleRequest | null) => {
      if (result) this.facade.updateAutoSendRule(rule.id, result);
    });
  }

  protected toggleRule(rule: WhatsAppAutoSendRule, enable: boolean): void {
    this.facade.updateAutoSendRule(rule.id, { isEnabled: enable });
  }

  protected confirmDeleteRule(rule: WhatsAppAutoSendRule): void {
    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Eliminar regla',
        message: `¿Eliminar la regla para "${this.getStudyStatusLabel(rule.studyStatus)}"? Esta acción no se puede deshacer.`,
        confirmText: 'Eliminar',
        confirmColor: 'warn',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) this.facade.deleteAutoSendRule(rule.id);
    });
  }
}
