import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiTextarea } from '../../../../shared/forms/textarea/textarea.component';
import {
  WhatsAppTemplate,
  WhatsAppTemplateTag,
  CreateWhatsAppTemplateRequest,
  UpdateWhatsAppTemplateRequest,
} from '../../models/whatsapp.models';

export interface TemplateFormDialogData {
  template?: WhatsAppTemplate;
  tags: WhatsAppTemplateTag[];
}

export type TemplateFormDialogResult = CreateWhatsAppTemplateRequest | UpdateWhatsAppTemplateRequest;

@Component({
  selector: 'app-template-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiInputText,
    UiDropdown,
    UiTextarea,
  ],
  templateUrl: './template-form-dialog.component.html',
  styleUrl: './template-form-dialog.component.scss'
})
export class TemplateFormDialog {
  private readonly dialogRef = inject(MatDialogRef<TemplateFormDialog>);
  private readonly data: TemplateFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faXmark = faXmark;
  protected readonly isEdit = !!this.data.template;

  protected readonly tagOptions: DropdownOption<string>[] = this.data.tags.map(t => ({
    value: t.tag,
    label: `${t.tag} — ${t.description}`,
  }));

  protected form = {
    name: this.data.template?.name ?? '',
    contentSid: this.data.template?.contentSid ?? '',
    description: this.data.template?.description ?? '',
    tags: this.data.template?.variables.map(v => v.tag) ?? [],
  };

  protected onSubmit(): void {
    const result: CreateWhatsAppTemplateRequest = {
      name: this.form.name,
      contentSid: this.form.contentSid,
      description: this.form.description || undefined,
      tags: this.form.tags,
    };
    this.dialogRef.close(result);
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
