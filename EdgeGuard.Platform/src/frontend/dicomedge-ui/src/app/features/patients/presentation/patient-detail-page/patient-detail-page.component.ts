import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft,
  faPen,
  faPhone,
  faEnvelope,
  faVenusMars,
  faCakeCandles,
  faHospital,
  faServer,
  faIdCard,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { UpdatePatientRequest } from '../../models/patient.models';
import { PatientsStore } from '../../services/patients.store';
import { PatientsFacade } from '../../services/patients.facade';
import { PatientFormDialog, PatientFormDialogData } from '../patient-form-dialog/patient-form-dialog.component';

import { StudiesApiService } from '../../../studies/infrastructure/studies-api.service';
import { Study } from '../../../studies/models/study.models';

@Component({
  selector: 'app-patient-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PatientsStore, PatientsFacade],
  imports: [
    MatCardModule,
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiLoadingSpinner,
    UiAlert,
    UiChip,
    UiDataTable,
    UiCellDef,
    UiStatusBadge,
    UiEmptyState,
    RelativeTimePipe,
    FileSizePipe,
  ],
  templateUrl: './patient-detail-page.component.html',
  styleUrl: './patient-detail-page.component.scss'
})
export default class PatientDetailPage {
  protected readonly facade = inject(PatientsFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly studiesApi = inject(StudiesApiService);

  protected readonly faArrowLeft = faArrowLeft;
  protected readonly faPen = faPen;
  protected readonly faPhone = faPhone;
  protected readonly faEnvelope = faEnvelope;
  protected readonly faVenusMars = faVenusMars;
  protected readonly faCakeCandles = faCakeCandles;
  protected readonly faHospital = faHospital;
  protected readonly faServer = faServer;
  protected readonly faIdCard = faIdCard;

  protected readonly patientStudies = signal<Study[]>([]);
  protected readonly studiesLoading = signal(false);

  protected readonly pageTitle = computed(() => {
    const p = this.facade.selectedPatient();
    return p ? `Paciente — ${p.patientName}` : 'Detalle de Paciente';
  });

  protected readonly studyColumns: TableColumn<Study>[] = [
    { key: 'studyDescription', header: 'Descripción', width: '30%' },
    { key: 'accessionNumber', header: 'Accession', width: '15%' },
    { key: 'status', header: 'Estado', width: '15%' },
    { key: 'totalSizeBytes', header: 'Tamaño', width: '12%', align: 'right' },
    { key: 'createdAt', header: 'Fecha', width: '15%' },
  ];

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.facade.loadPatientById(id);
      this.loadPatientStudies(id);
    }
  }

  protected goBack(): void {
    this.router.navigate(['/patients']);
  }

  protected goToStudy(study: Study): void {
    this.router.navigate(['/studies', study.id]);
  }

  protected openEditDialog(): void {
    const patient = this.facade.selectedPatient();
    if (!patient) return;

    this.dialog.open(PatientFormDialog, {
      data: { patient } satisfies PatientFormDialogData,
    }).afterClosed().subscribe((result: UpdatePatientRequest | null) => {
      if (result) {
        this.facade.updatePatient(patient.id, result);
      }
    });
  }

  private loadPatientStudies(patientId: string): void {
    this.studiesLoading.set(true);
    this.studiesApi.getByPatient(patientId).subscribe({
      next: (studies) => {
        this.patientStudies.set(studies);
        this.studiesLoading.set(false);
      },
      error: () => this.studiesLoading.set(false),
    });
  }
}
