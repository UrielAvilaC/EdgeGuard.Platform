import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { CdkDropList, CdkDrag, CdkDragHandle, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark, faFileLines, faPlus, faTrash, faGripVertical } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';
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
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
    FontAwesomeModule,
    UiButton,
    UiDialog,
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
  protected readonly faFileLines = faFileLines;
  protected readonly faPlus = faPlus;
  protected readonly faTrash = faTrash;
  protected readonly faGripVertical = faGripVertical;
  protected readonly isEdit = !!this.data.template;

  protected readonly tagOptions: DropdownOption<string>[] = this.data.tags.map(t => ({
    value: t.tag,
    label: `${t.tag} — ${t.description}`,
  }));

  /** Tag currently selected in the single-select dropdown, pending "Agregar". */
  protected selectedTag = '';

  protected form = {
    name: this.data.template?.name ?? '',
    contentSid: this.data.template?.contentSid ?? '',
    description: this.data.template?.description ?? '',
    // Array order === variable order. Variables arrive pre-sorted by position.
    tags: this.data.template?.variables.map(v => v.tag) ?? [],
  };

  /** Human-readable label for a tag value (falls back to the raw tag). */
  protected tagLabel(tag: string): string {
    return this.tagOptions.find(o => o.value === tag)?.label ?? tag;
  }

  /** Appends the selected tag as a new variable at the end (duplicates allowed). */
  protected addVariable(): void {
    if (!this.selectedTag) return;
    this.form.tags = [...this.form.tags, this.selectedTag];
    this.selectedTag = '';
  }

  protected removeVariable(index: number): void {
    this.form.tags = this.form.tags.filter((_, i) => i !== index);
  }

  /** Reorders the variable list via drag & drop; positions renumber automatically. */
  protected onDrop(event: CdkDragDrop<string[]>): void {
    const next = [...this.form.tags];
    moveItemInArray(next, event.previousIndex, event.currentIndex);
    this.form.tags = next;
  }

  protected get isFormValid(): boolean {
    // Variables are optional — a template may have zero variables.
    return !!this.form.name && !!this.form.contentSid;
  }

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
