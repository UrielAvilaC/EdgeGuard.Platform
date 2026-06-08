import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faMicrochip, faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import {
  Modality,
  NodeEquipment,
  CreateNodeEquipmentRequest,
} from '../../models/equipment.models';

export interface EquipmentFormDialogData {
  equipment?: NodeEquipment;
  nodeId: string;
  modalities: Modality[];
}

export type EquipmentFormDialogResult = CreateNodeEquipmentRequest;

@Component({
  selector: 'app-equipment-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiInputText,
  ],
  templateUrl: './equipment-form-dialog.component.html',
})
export class EquipmentFormDialog {
  private readonly dialogRef = inject(MatDialogRef<EquipmentFormDialog>);
  readonly data: EquipmentFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faMicrochip = faMicrochip;
  protected readonly faXmark = faXmark;
  protected readonly isEdit = !!this.data.equipment;

  /** Supported & active modalities offered for assignment. */
  protected readonly modalities = this.data.modalities;

  /** Selected modality codes (Set for O(1) toggle). */
  protected readonly selected = signal<Set<string>>(
    new Set(this.data.equipment?.modalityCodes ?? []),
  );

  protected form = {
    aeTitle:        this.data.equipment?.aeTitle ?? '',
    displayName:    this.data.equipment?.displayName ?? '',
    stationAeTitle: this.data.equipment?.stationAeTitle ?? '',
    stationName:    this.data.equipment?.stationName ?? '',
    ipAddress:      this.data.equipment?.ipAddress ?? '',
    location:       this.data.equipment?.location ?? '',
    department:     this.data.equipment?.department ?? '',
    manufacturer:   this.data.equipment?.manufacturer ?? '',
    model:          this.data.equipment?.model ?? '',
    notes:          this.data.equipment?.notes ?? '',
  };

  protected isSelected(code: string): boolean {
    return this.selected().has(code);
  }

  protected toggleModality(code: string): void {
    this.selected.update((s) => {
      const next = new Set(s);
      next.has(code) ? next.delete(code) : next.add(code);
      return next;
    });
  }

  protected get isAeTitleValid(): boolean {
    return /^[A-Z0-9_]{1,16}$/.test(this.form.aeTitle.trim());
  }

  protected get isFormValid(): boolean {
    return this.isAeTitleValid && this.selected().size > 0;
  }

  protected onSubmit(): void {
    if (!this.isFormValid) return;

    const result: EquipmentFormDialogResult = {
      aeTitle:        this.form.aeTitle.trim().toUpperCase(),
      displayName:    this.form.displayName.trim() || undefined,
      modalityCodes:  [...this.selected()],
      stationAeTitle: this.form.stationAeTitle.trim() || undefined,
      stationName:    this.form.stationName.trim() || undefined,
      ipAddress:      this.form.ipAddress.trim() || undefined,
      location:       this.form.location.trim() || undefined,
      department:     this.form.department.trim() || undefined,
      manufacturer:   this.form.manufacturer.trim() || undefined,
      model:          this.form.model.trim() || undefined,
      notes:          this.form.notes.trim() || undefined,
    };

    this.dialogRef.close(result);
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
