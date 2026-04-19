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

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header';
import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { UpdatePatientRequest } from '../../models/patient.models';
import { PatientsStore } from '../../services/patients.store';
import { PatientsFacade } from '../../services/patients.facade';
import { PatientFormDialog, PatientFormDialogData } from '../patient-form-dialog/patient-form-dialog';

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
  template: `
    <div class="space-y-6">
      <ui-page-header [title]="pageTitle()">
        <ui-button variant="ghost" size="sm" [icon]="faArrowLeft" (clicked)="goBack()">
          Volver a pacientes
        </ui-button>
        @if (facade.selectedPatient()) {
          <ui-button variant="secondary" size="sm" [icon]="faPen" (clicked)="openEditDialog()">
            Editar
          </ui-button>
        }
      </ui-page-header>

      @if (facade.selectedLoading()) {
        <ui-loading-spinner [overlay]="true" />
      }

      @if (facade.error(); as error) {
        <ui-alert type="error" [title]="error" />
      }

      @if (facade.selectedPatient(); as p) {
        <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
          <!-- Main Info -->
          <div class="xl:col-span-2 space-y-6">
            <!-- Demographics -->
            <mat-card class="!p-6">
              <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-6">
                Datos Demográficos
              </h3>
              <dl class="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-4">
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Nombre completo</dt>
                  <dd class="mt-1 text-sm font-medium text-gray-900 dark:text-white flex items-center gap-2">
                    {{ p.patientName }}
                    @if (!p.isActive) {
                      <ui-chip color="default">Inactivo</ui-chip>
                    }
                  </dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faIdCard" class="mr-1" /> DICOM ID
                  </dt>
                  <dd class="mt-1 text-sm font-mono text-gray-900 dark:text-white">{{ p.patientDicomId }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faCakeCandles" class="mr-1" /> Fecha de Nacimiento
                  </dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ p.birthDate ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faVenusMars" class="mr-1" /> Sexo
                  </dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">
                    {{ p.sex === 'M' ? 'Masculino' : p.sex === 'F' ? 'Femenino' : p.sex ?? '—' }}
                  </dd>
                </div>
              </dl>
            </mat-card>

            <!-- Associated Studies -->
            <mat-card class="!p-6">
              <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Estudios Asociados
              </h3>
              @if (studiesLoading()) {
                <ui-loading-spinner [diameter]="32" />
              } @else if (patientStudies().length === 0) {
                <ui-empty-state title="Sin estudios" description="Este paciente no tiene estudios registrados." />
              } @else {
                <ui-data-table
                  [columns]="studyColumns"
                  [data]="patientStudies()"
                  emptyMessage="Sin estudios."
                  (rowClick)="goToStudy($event)"
                >
                  <ng-template [uiCellDef]="'status'" let-value="value">
                    <ui-status-badge [status]="value" />
                  </ng-template>

                  <ng-template [uiCellDef]="'totalSizeBytes'" let-value="value">
                    {{ value | fileSize }}
                  </ng-template>

                  <ng-template [uiCellDef]="'createdAt'" let-value="value">
                    {{ value | relativeTime }}
                  </ng-template>
                </ui-data-table>
              }
            </mat-card>
          </div>

          <!-- Sidebar -->
          <div class="space-y-6">
            <!-- Contact -->
            <mat-card class="!p-6">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
                Contacto
              </h3>
              <dl class="space-y-3">
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faPhone" class="mr-1" /> Teléfono
                  </dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ p.phoneNumber ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faEnvelope" class="mr-1" /> Email
                  </dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ p.email ?? '—' }}</dd>
                </div>
              </dl>
            </mat-card>

            <!-- Source -->
            <mat-card class="!p-6">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
                Origen
              </h3>
              <dl class="space-y-3">
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faHospital" class="mr-1" /> Facility
                  </dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ p.facilitySource ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faServer" class="mr-1" /> Nodo creador
                  </dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ p.createdByNodeId ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Issuer of Patient ID</dt>
                  <dd class="mt-1 text-sm font-mono text-gray-900 dark:text-white">{{ p.issuerOfPatientId ?? '—' }}</dd>
                </div>
              </dl>
            </mat-card>

            <!-- Timestamps -->
            <mat-card class="!p-6">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
                Timestamps
              </h3>
              <dl class="space-y-3">
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Creado</dt>
                  <dd class="text-sm text-gray-900 dark:text-white">{{ p.createdAt | relativeTime }}</dd>
                </div>
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Actualizado</dt>
                  <dd class="text-sm text-gray-900 dark:text-white">{{ p.updatedAt ? (p.updatedAt | relativeTime) : '—' }}</dd>
                </div>
              </dl>
            </mat-card>
          </div>
        </div>
      }
    </div>
  `,
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
