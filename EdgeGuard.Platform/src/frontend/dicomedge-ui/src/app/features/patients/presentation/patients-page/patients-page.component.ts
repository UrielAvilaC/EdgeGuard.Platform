import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faFileExport,
  faFileImport,
  faSync,
  faPhone,
  faEnvelope,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { Patient } from '../../models/patient.models';
import { PatientsStore } from '../../services/patients.store';
import { PatientsFacade } from '../../services/patients.facade';
import { PatientFilters } from '../patient-filters/patient-filters.component';

@Component({
  selector: 'app-patients-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PatientsStore, PatientsFacade],
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiDataTable,
    UiCellDef,
    UiChip,
    UiAlert,
    RelativeTimePipe,
    PatientFilters,
  ],
  templateUrl: './patients-page.component.html',
  styleUrl: './patients-page.component.scss'
})
export default class PatientsPage {
  protected readonly facade = inject(PatientsFacade);
  private readonly router = inject(Router);

  protected readonly faFileExport = faFileExport;
  protected readonly faFileImport = faFileImport;
  protected readonly faSync = faSync;
  protected readonly faPhone = faPhone;
  protected readonly faEnvelope = faEnvelope;

  protected readonly columns: TableColumn<Patient>[] = [
    { key: 'patientName', header: 'Nombre', sortable: true, width: '22%' },
    { key: 'patientDicomId', header: 'DICOM ID', sortable: true, width: '15%' },
    { key: 'sex', header: 'Sexo', sortable: true, width: '10%' },
    { key: 'birthDate', header: 'Fecha Nac.', sortable: true, width: '12%' },
    { key: 'phoneNumber', header: 'Teléfono', width: '13%' },
    { key: 'email', header: 'Email', width: '16%' },
    { key: 'createdAt', header: 'Registrado', sortable: true, width: '12%' },
  ];

  constructor() {
    this.facade.loadPatients();
  }

  protected onRowClick(patient: Patient): void {
    this.router.navigate(['/patients', patient.id]);
  }

  protected onImportFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.facade.importCsv(file);
      input.value = '';
    }
  }
}
