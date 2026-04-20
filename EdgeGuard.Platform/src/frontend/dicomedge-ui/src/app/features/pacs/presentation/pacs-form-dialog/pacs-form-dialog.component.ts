import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { PacsServer, CreatePacsServerRequest, UpdatePacsServerRequest } from '../../models/pacs.models';

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
  ],
  templateUrl: './pacs-form-dialog.component.html',
  styleUrl: './pacs-form-dialog.component.scss'
})
export class PacsFormDialog {
  private readonly dialogRef = inject(MatDialogRef<PacsFormDialog>);
  private readonly data: PacsFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faXmark = faXmark;
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
