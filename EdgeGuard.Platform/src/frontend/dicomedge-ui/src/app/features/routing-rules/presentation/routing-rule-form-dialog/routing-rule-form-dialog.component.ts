import { ChangeDetectionStrategy, Component, inject, ViewChild } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark, faRoute } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { RoutingRule, CreateRoutingRuleRequest, UpdateRoutingRuleRequest } from '../../models/routing-rule.models';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

export interface RoutingRuleFormDialogData {
  rule?: RoutingRule;
  nodeOptions: { id: string; name: string }[];
}

export type RoutingRuleFormDialogResult = CreateRoutingRuleRequest | UpdateRoutingRuleRequest;

@Component({
  selector: 'app-routing-rule-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiInputText,
    UiDropdown,
    UiDialog,
  ],
  templateUrl: './routing-rule-form-dialog.component.html',
  styleUrl: './routing-rule-form-dialog.component.scss',
})
export class RoutingRuleFormDialog {
  private readonly dialogRef = inject(MatDialogRef<RoutingRuleFormDialog>);
  private readonly data: RoutingRuleFormDialogData = inject(MAT_DIALOG_DATA);

  @ViewChild('ruleForm') formRef!: NgForm;

  protected readonly faXmark = faXmark;
  protected readonly faRoute = faRoute;
  protected readonly isEdit = !!this.data.rule;

  protected readonly nodeDropdownOptions: DropdownOption<string>[] = this.data.nodeOptions.map(n => ({
    value: n.id,
    label: n.name,
  }));

  protected form = {
    name: this.data.rule?.name ?? '',
    targetNodeId: this.data.rule?.targetNodeId ?? '',
    priority: this.data.rule?.priority ?? 100,
    matchMessageType: this.data.rule?.matchMessageType ?? '',
    matchTriggerEvent: this.data.rule?.matchTriggerEvent ?? '',
    matchSendingFacility: this.data.rule?.matchSendingFacility ?? '',
    matchSendingApplication: this.data.rule?.matchSendingApplication ?? '',
  };

  protected get isFormValid(): boolean {
    return !!this.form.name && !!this.form.targetNodeId && this.form.priority > 0;
  }

  protected onSubmit(): void {
    if (!this.isFormValid) return;
    const result = {
      name: this.form.name,
      targetNodeId: this.form.targetNodeId,
      priority: this.form.priority,
      matchMessageType: this.form.matchMessageType || undefined,
      matchTriggerEvent: this.form.matchTriggerEvent || undefined,
      matchSendingFacility: this.form.matchSendingFacility || undefined,
      matchSendingApplication: this.form.matchSendingApplication || undefined,
    };
    this.dialogRef.close(result);
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
