import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark, faCodeBranch } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { RoutingRule, CreateRoutingRuleRequest, UpdateRoutingRuleRequest } from '../../models/hl7.models';
import { Node } from '../../../nodes/models/node.models';

export interface RoutingRuleFormDialogData {
  rule?: RoutingRule;
  nodes: Node[];
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
  ],
  templateUrl: './routing-rule-form-dialog.component.html',
  styleUrl: './routing-rule-form-dialog.component.scss'
})
export class RoutingRuleFormDialog {
  private readonly dialogRef = inject(MatDialogRef<RoutingRuleFormDialog>);
  private readonly data: RoutingRuleFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faXmark = faXmark;
  protected readonly faCodeBranch = faCodeBranch;
  protected readonly isEdit = !!this.data.rule;

  protected readonly nodeOptions: DropdownOption<string>[] = this.data.nodes.map(n => ({
    value: n.id,
    label: `${n.name} (${n.aeTitle})`,
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
    return !!this.form.name && !!this.form.targetNodeId;
  }

  protected onSubmit(): void {
    const result: CreateRoutingRuleRequest = {
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
