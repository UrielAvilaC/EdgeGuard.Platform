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

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header';
import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { Patient } from '../../models/patient.models';
import { PatientsStore } from '../../services/patients.store';
import { PatientsFacade } from '../../services/patients.facade';
import { PatientFilters } from '../patient-filters/patient-filters';

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
  template: `
    <div class="space-y-6">
      <ui-page-header
        title="Pacientes"
        description="Gestión de datos demográficos de pacientes DICOM."
      >
        <ui-button variant="secondary" size="sm" [icon]="faSync" (clicked)="facade.loadPatients()">
          Actualizar
        </ui-button>
        <ui-button variant="secondary" size="sm" [icon]="faFileExport" (clicked)="facade.exportCsv()">
          Exportar
        </ui-button>
        <ui-button variant="secondary" size="sm" [icon]="faFileImport" (clicked)="fileInput.click()">
          Importar
        </ui-button>
        <input
          #fileInput
          type="file"
          accept=".csv"
          class="hidden"
          (change)="onImportFile($event)"
          aria-label="Seleccionar archivo CSV para importar"
        />
      </ui-page-header>

      @if (facade.error(); as error) {
        <ui-alert type="error" [title]="error" [dismissible]="true" />
      }

      <app-patient-filters
        [currentFilter]="facade.filter()"
        [activeFilterCount]="facade.activeFilterCount()"
        (filterChange)="facade.updateFilter($event)"
        (clearFilters)="facade.clearFilters()"
      />

      <ui-data-table
        [columns]="columns"
        [data]="facade.patients()"
        [pagination]="facade.pagination()"
        [sort]="facade.sort()"
        [loading]="facade.loading()"
        emptyMessage="No se encontraron pacientes con los filtros aplicados."
        (pageChange)="facade.changePage($event.page, $event.pageSize)"
        (sortChange)="facade.changeSort($event)"
        (rowClick)="onRowClick($event)"
      >
        <ng-template [uiCellDef]="'patientName'" let-row let-value="value">
          <div class="flex items-center gap-2">
            <span class="font-medium text-gray-900 dark:text-white">{{ value }}</span>
            @if (!row.isActive) {
              <ui-chip color="default">Inactivo</ui-chip>
            }
          </div>
        </ng-template>

        <ng-template [uiCellDef]="'sex'" let-value="value">
          {{ value === 'M' ? 'Masculino' : value === 'F' ? 'Femenino' : value ?? '—' }}
        </ng-template>

        <ng-template [uiCellDef]="'phoneNumber'" let-value="value">
          @if (value) {
            <span class="inline-flex items-center gap-1 text-sm">
              <fa-icon [icon]="faPhone" class="text-xs text-gray-400" />
              {{ value }}
            </span>
          } @else {
            <span class="text-gray-400">—</span>
          }
        </ng-template>

        <ng-template [uiCellDef]="'email'" let-value="value">
          @if (value) {
            <span class="inline-flex items-center gap-1 text-sm">
              <fa-icon [icon]="faEnvelope" class="text-xs text-gray-400" />
              {{ value }}
            </span>
          } @else {
            <span class="text-gray-400">—</span>
          }
        </ng-template>

        <ng-template [uiCellDef]="'createdAt'" let-value="value">
          {{ value | relativeTime }}
        </ng-template>
      </ui-data-table>
    </div>
  `,
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
