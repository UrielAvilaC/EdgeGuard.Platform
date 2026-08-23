import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiDatepicker } from '../../../../shared/forms/datepicker/datepicker.component';
import { UiDropdown } from '../../../../shared/forms/dropdown/dropdown.component';
import { Patient, UpdatePatientRequest, SEX_OPTIONS } from '../../models/patient.models';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

export interface PatientFormDialogData {
  patient: Patient;
}

@Component({
  selector: 'app-patient-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiInputText,
    UiDatepicker,
    UiDropdown,
    UiDialog,
  ],
  templateUrl: './patient-form-dialog.component.html',
  styleUrl: './patient-form-dialog.component.scss'
})
export class PatientFormDialog {
  private readonly dialogRef = inject(MatDialogRef<PatientFormDialog>);
  private readonly data: PatientFormDialogData = inject(MAT_DIALOG_DATA);

  protected readonly faXmark = faXmark;
  protected readonly sexOptions = SEX_OPTIONS;

  protected form: UpdatePatientRequest = {
    patientName: this.data.patient.patientName,
    birthDate: this.data.patient.birthDate ?? undefined,
    sex: this.data.patient.sex ?? undefined,
    phoneNumber: this.data.patient.phoneNumber ?? undefined,
    email: this.data.patient.email ?? undefined,
  };

  protected onSubmit(): void {
    this.dialogRef.close(this.form);
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
