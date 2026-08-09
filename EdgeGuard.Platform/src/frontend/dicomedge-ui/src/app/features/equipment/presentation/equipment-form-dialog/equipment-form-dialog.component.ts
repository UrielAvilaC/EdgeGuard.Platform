import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faChevronDown, faMicrochip, faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { DropdownOption, UiDropdown } from '../../../../shared/forms/dropdown/dropdown.component';
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
    UiDropdown,
  ],
  templateUrl: './equipment-form-dialog.component.html',
})
export class EquipmentFormDialog {
  private readonly dialogRef = inject(MatDialogRef<EquipmentFormDialog>);
  readonly data: EquipmentFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faMicrochip = faMicrochip;
  protected readonly faXmark = faXmark;
  protected readonly faChevronDown = faChevronDown;
  protected readonly isEdit = !!this.data.equipment;

  /** Optional sections start collapsed unless the equipment already has data there. */
  protected stationExpanded = !!(this.data.equipment?.stationAeTitle || this.data.equipment?.stationName);
  protected inventoryExpanded = !!(
    this.data.equipment?.location ||
    this.data.equipment?.department ||
    this.data.equipment?.manufacturer ||
    this.data.equipment?.model ||
    this.data.equipment?.notes
  );

  /** Supported & active modalities offered for assignment. */
  protected readonly modalityOptions: DropdownOption<string>[] = this.data.modalities.map((m) => ({
    value: m.code,
    label: `${m.code} — ${m.displayName}`,
  }));

  protected form = {
    aeTitle:        this.data.equipment?.aeTitle ?? '',
    modalityCodes:  [...(this.data.equipment?.modalityCodes ?? [])] as string[],
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

  protected get isAeTitleValid(): boolean {
    return /^[A-Z0-9_]{1,16}$/.test(this.form.aeTitle.trim());
  }

  /** IPv4 dotted-quad; required because the node grants association permission by IP. */
  protected get isIpAddressValid(): boolean {
    return /^((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$/.test(
      this.form.ipAddress.trim(),
    );
  }

  protected get isFormValid(): boolean {
    return /*this.isAeTitleValid && */this.form.modalityCodes.length > 0 && this.isIpAddressValid;
  }

  protected onSubmit(): void {
    if (!this.isFormValid) return;

    const result: EquipmentFormDialogResult = {
      aeTitle:        this.form.aeTitle.trim().toUpperCase(),
      displayName:    this.form.displayName.trim() || undefined,
      modalityCodes:  [...this.form.modalityCodes],
      stationAeTitle: this.form.stationAeTitle.trim() || undefined,
      stationName:    this.form.stationName.trim() || undefined,
      ipAddress:      this.form.ipAddress.trim(),
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
