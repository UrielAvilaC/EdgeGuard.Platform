import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button';
import { UiInputText } from '../../../../shared/forms/input-text/input-text';
import { UiDatepicker } from '../../../../shared/forms/datepicker/datepicker';
import { UiDropdown } from '../../../../shared/forms/dropdown/dropdown';
import { Patient, UpdatePatientRequest, SEX_OPTIONS } from '../../models/patient.models';

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
  ],
  template: `
    <div class="p-6 w-[500px] max-w-full">
      <div class="flex items-center justify-between mb-6">
        <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Editar Paciente</h2>
        <ui-icon-button [icon]="faXmark" tooltip="Cerrar" ariaLabel="Cerrar diálogo" (clicked)="onCancel()" />
      </div>

      <form #patientForm="ngForm" (ngSubmit)="onSubmit()" class="space-y-4">
        <ui-input-text
          label="Nombre"
          placeholder="Nombre del paciente"
          [(ngModel)]="form.patientName"
          name="patientName"
          required
        />

        <ui-datepicker
          label="Fecha de Nacimiento"
          [(ngModel)]="form.birthDate"
          name="birthDate"
        />

        <ui-dropdown
          label="Sexo"
          [options]="sexOptions"
          [showEmpty]="true"
          emptyLabel="No especificado"
          [(ngModel)]="form.sex"
          name="sex"
        />

        <ui-input-text
          label="Teléfono"
          placeholder="+52 55 1234 5678"
          [(ngModel)]="form.phoneNumber"
          name="phoneNumber"
        />

        <ui-input-text
          label="Email"
          placeholder="paciente@ejemplo.com"
          type="email"
          [(ngModel)]="form.email"
          name="email"
        />

        <div class="flex justify-end gap-3 pt-4 border-t border-gray-200 dark:border-gray-700">
          <ui-button variant="secondary" type="button" (clicked)="onCancel()">
            Cancelar
          </ui-button>
          <ui-button type="submit">
            Guardar
          </ui-button>
        </div>
      </form>
    </div>
  `,
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
