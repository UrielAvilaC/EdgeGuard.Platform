import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faRoute, faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import {
  NodeDicomRoutingRule,
  CreateNodeDicomRoutingRuleRequest,
  DICOM_MODALITY_OPTIONS,
} from '../../models/node-dicom-routing-rule.models';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

export interface NodeDicomRuleFormDialogData {
  rule?: NodeDicomRoutingRule;
  destinationAeTitle: string;
  nodeId: string;
}

export type NodeDicomRuleFormDialogResult = CreateNodeDicomRoutingRuleRequest;

@Component({
  selector: 'app-node-dicom-rule-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiInputText,
    UiDropdown,
    UiSlideToggle,
    UiDialog,
  ],
  templateUrl: './node-dicom-rule-form-dialog.component.html',
})
export class NodeDicomRuleFormDialog {
  private readonly dialogRef = inject(MatDialogRef<NodeDicomRuleFormDialog>);
  readonly data: NodeDicomRuleFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faRoute = faRoute;
  protected readonly faXmark = faXmark;
  protected readonly isEdit = !!this.data.rule;

  protected readonly modalityOptions: DropdownOption<string>[] = [
    { value: '', label: 'Cualquier modalidad' },
    ...DICOM_MODALITY_OPTIONS.map(m => ({ value: m.value, label: m.label })),
  ];

  protected form = {
    name:                 this.data.rule?.name ?? '',
    priority:             this.data.rule?.priority ?? 100,
    matchModality:        this.data.rule?.matchModality ?? '',
    matchSourceAeTitle:   this.data.rule?.matchSourceAeTitle ?? '',
    matchInstitution:     this.data.rule?.matchInstitution ?? '',
    matchStudyDesc:       this.data.rule?.matchStudyDesc ?? '',
    minInstanceCount:     this.data.rule?.minInstanceCount ?? null as number | null,
    maxInstanceCount:     this.data.rule?.maxInstanceCount ?? null as number | null,
    sendToPacs:           this.data.rule?.sendToPacs ?? true,
    sendToHub:            this.data.rule?.sendToHub ?? false,
    anonymizeBeforeSend:  this.data.rule?.anonymizeBeforeSend ?? false,
  };

  protected get isFormValid(): boolean {
    return !!this.form.name.trim() && this.form.priority > 0 &&
      (this.form.sendToPacs || this.form.sendToHub);
  }

  protected onSubmit(): void {
    if (!this.isFormValid) return;

    const result: NodeDicomRuleFormDialogResult = {
      name:                  this.form.name.trim(),
      priority:              this.form.priority,
      destinationAeTitle:    this.data.destinationAeTitle,
      sendToPacs:            this.form.sendToPacs,
      sendToHub:             this.form.sendToHub,
      anonymizeBeforeSending: this.form.anonymizeBeforeSend,
      matchModality:         this.form.matchModality || undefined,
      matchSourceAeTitle:    this.form.matchSourceAeTitle || undefined,
      matchInstitution:      this.form.matchInstitution || undefined,
      matchStudyDescContains: this.form.matchStudyDesc || undefined,
      minInstanceCount:      this.form.minInstanceCount ?? undefined,
      maxInstanceCount:      this.form.maxInstanceCount ?? undefined,
    };

    this.dialogRef.close(result);
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
