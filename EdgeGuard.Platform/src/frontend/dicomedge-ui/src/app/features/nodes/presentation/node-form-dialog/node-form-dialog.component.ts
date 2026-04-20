import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { Node, CreateNodeRequest, UpdateNodeRequest } from '../../models/node.models';

export interface NodeFormDialogData {
  node?: Node;
}

export type NodeFormDialogResult = CreateNodeRequest | UpdateNodeRequest;

@Component({
  selector: 'app-node-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiInputText,
  ],
  templateUrl: './node-form-dialog.component.html',
  styleUrl: './node-form-dialog.component.scss'
})
export class NodeFormDialog {
  private readonly dialogRef = inject(MatDialogRef<NodeFormDialog>);
  private readonly data: NodeFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faXmark = faXmark;
  protected readonly isEdit = !!this.data.node;

  protected createForm: CreateNodeRequest = {
    name: '',
    aeTitle: '',
    ipAddress: '',
    port: 104,
    apiEndpoint: undefined,
    location: this.data.node?.location ?? undefined,
    facilityName: this.data.node?.facilityName ?? undefined,
    healthCheckIntervalSeconds: 60,
  };

  protected editForm: UpdateNodeRequest = {
    location: this.data.node?.location ?? undefined,
    facilityName: this.data.node?.facilityName ?? undefined,
    timeZone: undefined,
    healthCheckIntervalSeconds: this.data.node?.healthCheckIntervalSeconds,
    maxStorageMb: this.data.node?.maxStorageMb,
  };

  protected onSubmit(): void {
    if (this.isEdit) {
      this.dialogRef.close(this.editForm);
    } else {
      const result: CreateNodeRequest = {
        ...this.createForm,
        location: this.editForm.location,
        facilityName: this.editForm.facilityName,
      };
      this.dialogRef.close(result);
    }
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
