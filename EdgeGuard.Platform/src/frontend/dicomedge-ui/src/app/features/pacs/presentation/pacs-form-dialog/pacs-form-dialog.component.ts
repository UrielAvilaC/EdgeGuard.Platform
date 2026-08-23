import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark, faDatabase } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { PacsServer, CreatePacsServerRequest, UpdatePacsServerRequest } from '../../models/pacs.models';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

export interface PacsFormDialogData {
  server?: PacsServer;
}

export type PacsFormDialogResult = CreatePacsServerRequest | UpdatePacsServerRequest;

@Component({
  selector: 'app-pacs-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiInputText,
    UiSlideToggle,
    UiDialog,
  ],
  templateUrl: './pacs-form-dialog.component.html',
  styleUrl: './pacs-form-dialog.component.scss'
})
export class PacsFormDialog {
  private readonly dialogRef = inject(MatDialogRef<PacsFormDialog>);
  private readonly data: PacsFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faXmark = faXmark;
  protected readonly faDatabase = faDatabase;
  protected readonly isEdit = !!this.data.server;

  protected createAeTitle = '';
  protected createIsGlobal = false;

  protected form = {
    name: this.data.server?.name ?? '',
    hostName: this.data.server?.hostName ?? '',
    port: this.data.server?.port ?? 104,
    description: this.data.server?.description ?? undefined,
    maxConcurrentAssociations: this.data.server?.maxConcurrentAssociations ?? 10,
    timeoutSeconds: this.data.server?.timeoutSeconds ?? 30,
  };

  protected get isFormValid(): boolean {
    if (this.isEdit) return !!this.form.name && !!this.form.hostName && this.form.port > 0;
    return !!this.form.name && !!this.createAeTitle && !!this.form.hostName && this.form.port > 0;
  }

  protected onSubmit(): void {
    if (this.isEdit) {
      const result: UpdatePacsServerRequest = {
        name: this.form.name,
        hostName: this.form.hostName,
        port: this.form.port,
        description: this.form.description,
        maxConcurrentAssociations: this.form.maxConcurrentAssociations,
        timeoutSeconds: this.form.timeoutSeconds,
      };
      this.dialogRef.close(result);
    } else {
      const result: CreatePacsServerRequest = {
        name: this.form.name,
        aeTitle: this.createAeTitle,
        hostName: this.form.hostName,
        port: this.form.port,
        description: this.form.description,
        isGlobal: this.createIsGlobal,
        maxConcurrentAssociations: this.form.maxConcurrentAssociations,
        timeoutSeconds: this.form.timeoutSeconds,
      };
      this.dialogRef.close(result);
    }
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
