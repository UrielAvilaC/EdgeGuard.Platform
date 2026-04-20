import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark, faPlus, faTrash, faPaperPlane } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import {
  WhatsAppTemplate,
  WhatsAppRecipient,
  SendWhatsAppManualRequest,
} from '../../models/whatsapp.models';

export interface SendManualDialogData {
  studyId: string;
  templates: WhatsAppTemplate[];
}

@Component({
  selector: 'app-send-manual-dialog',
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
  templateUrl: './send-manual-dialog.component.html',
  styleUrl: './send-manual-dialog.component.scss'
})
export class SendManualDialog {
  private readonly dialogRef = inject(MatDialogRef<SendManualDialog>);
  protected readonly data: SendManualDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faXmark = faXmark;
  protected readonly faPlus = faPlus;
  protected readonly faTrash = faTrash;
  protected readonly faPaperPlane = faPaperPlane;

  protected readonly templateOptions: DropdownOption<string>[] = this.data.templates
    .filter(t => t.isActive)
    .map(t => ({ value: t.id, label: t.name }));

  protected templateId = '';
  protected recipients: WhatsAppRecipient[] = [{ phoneNumber: '' }];

  protected get isFormValid(): boolean {
    return !!this.templateId && this.recipients.some(r => r.phoneNumber.trim().length > 0);
  }

  protected addRecipient(): void {
    this.recipients = [...this.recipients, { phoneNumber: '' }];
  }

  protected removeRecipient(index: number): void {
    this.recipients = this.recipients.filter((_, i) => i !== index);
  }

  protected onSubmit(): void {
    const result: SendWhatsAppManualRequest = {
      studyId: this.data.studyId,
      templateId: this.templateId,
      recipients: this.recipients.filter(r => r.phoneNumber.trim()),
    };
    this.dialogRef.close(result);
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
