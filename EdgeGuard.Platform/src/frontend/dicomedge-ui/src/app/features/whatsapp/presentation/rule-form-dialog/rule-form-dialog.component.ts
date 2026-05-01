import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark, faRobot } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiTextarea } from '../../../../shared/forms/textarea/textarea.component';
import {
  WhatsAppAutoSendRule,
  WhatsAppTemplate,
  CreateWhatsAppAutoSendRuleRequest,
  UpdateWhatsAppAutoSendRuleRequest,
  STUDY_STATUS_OPTIONS,
} from '../../models/whatsapp.models';

export interface RuleFormDialogData {
  rule?: WhatsAppAutoSendRule;
  templates: WhatsAppTemplate[];
}

export type RuleFormDialogResult = CreateWhatsAppAutoSendRuleRequest | UpdateWhatsAppAutoSendRuleRequest;

@Component({
  selector: 'app-rule-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiDropdown,
    UiTextarea,
  ],
  templateUrl: './rule-form-dialog.component.html',
  styleUrl: './rule-form-dialog.component.scss'
})
export class RuleFormDialog {
  private readonly dialogRef = inject(MatDialogRef<RuleFormDialog>);
  private readonly data: RuleFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faXmark = faXmark;
  protected readonly faRobot = faRobot;
  protected readonly isEdit = !!this.data.rule;

  protected readonly studyStatusOptions: DropdownOption<string>[] = STUDY_STATUS_OPTIONS.map(s => ({
    value: s.value,
    label: s.label,
  }));

  protected readonly templateOptions: DropdownOption<string>[] = this.data.templates.map(t => ({
    value: t.id,
    label: t.name,
  }));

  protected createForm = {
    studyStatus: this.data.rule?.studyStatus ?? '',
  };

  protected templateId = this.data.rule?.templateId ?? '';
  protected description = this.data.rule?.description ?? '';

  protected get isFormValid(): boolean {
    if (this.isEdit) return true;
    return !!this.createForm.studyStatus && !!this.templateId;
  }

  protected onSubmit(): void {
    if (this.isEdit) {
      const result: UpdateWhatsAppAutoSendRuleRequest = {
        templateId: this.templateId || undefined,
        description: this.description || undefined,
      };
      this.dialogRef.close(result);
    } else {
      const result: CreateWhatsAppAutoSendRuleRequest = {
        studyStatus: this.createForm.studyStatus,
        templateId: this.templateId,
        description: this.description || undefined,
      };
      this.dialogRef.close(result);
    }
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
