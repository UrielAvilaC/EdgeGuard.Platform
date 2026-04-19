import { ChangeDetectionStrategy, Component, inject, TemplateRef, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faFileExport,
  faExclamationTriangle,
  faSync,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header';
import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { TruncatePipe } from '../../../../shared/pipes/truncate.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { Study } from '../../models/study.models';
import { StudiesStore } from '../../services/studies.store';
import { StudiesFacade } from '../../services/studies.facade';
import { StudyFilters } from '../study-filters/study-filters';

@Component({
  selector: 'app-studies-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [StudiesStore, StudiesFacade],
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiDataTable,
    UiCellDef,
    UiStatusBadge,
    UiChip,
    UiAlert,
    RelativeTimePipe,
    FileSizePipe,
    TruncatePipe,
    StudyFilters,
  ],
  template: `
    <div class="space-y-6">
      <ui-page-header
        title="Estudios DICOM"
        description="Gestión y monitoreo de estudios recibidos, procesados y enviados a PACS."
      >
        <ui-button variant="secondary" size="sm" [icon]="faSync" (clicked)="facade.loadStudies()">
          Actualizar
        </ui-button>
        <ui-button variant="secondary" size="sm" [icon]="faFileExport" (clicked)="facade.exportCsv()">
          Exportar CSV
        </ui-button>
      </ui-page-header>

      @if (facade.error(); as error) {
        <ui-alert type="error" [title]="error" [dismissible]="true" />
      }

      <app-study-filters
        [currentFilter]="facade.filter()"
        [activeFilterCount]="facade.activeFilterCount()"
        (filterChange)="facade.updateFilter($event)"
        (clearFilters)="facade.clearFilters()"
      />

      <ui-data-table
        [columns]="columns"
        [data]="facade.studies()"
        [pagination]="facade.pagination()"
        [sort]="facade.sort()"
        [loading]="facade.loading()"
        emptyMessage="No se encontraron estudios con los filtros aplicados."
        (pageChange)="facade.changePage($event.page, $event.pageSize)"
        (sortChange)="facade.changeSort($event)"
        (rowClick)="onRowClick($event)"
      >
        <ng-template [uiCellDef]="'patientName'" let-row let-value="value">
          <div class="flex items-center gap-2">
            <span class="font-medium text-gray-900 dark:text-white">{{ value }}</span>
            @if (row.isUrgent) {
              <ui-chip color="danger">
                <fa-icon [icon]="faExclamationTriangle" class="mr-1 text-xs" />
                Urgente
              </ui-chip>
            }
          </div>
        </ng-template>

        <ng-template [uiCellDef]="'studyDescription'" let-value="value">
          {{ value | truncate:60 }}
        </ng-template>

        <ng-template [uiCellDef]="'status'" let-value="value">
          <ui-status-badge [status]="value" />
        </ng-template>

        <ng-template [uiCellDef]="'totalSizeBytes'" let-value="value">
          {{ value | fileSize }}
        </ng-template>

        <ng-template [uiCellDef]="'seriesCount'" let-row>
          {{ row.seriesCount }}s / {{ row.instanceCount }}i
        </ng-template>

        <ng-template [uiCellDef]="'createdAt'" let-value="value">
          {{ value | relativeTime }}
        </ng-template>
      </ui-data-table>
    </div>
  `,
})
export default class StudiesPage {
  protected readonly facade = inject(StudiesFacade);
  private readonly router = inject(Router);

  protected readonly faFileExport = faFileExport;
  protected readonly faExclamationTriangle = faExclamationTriangle;
  protected readonly faSync = faSync;

  protected readonly columns: TableColumn<Study>[] = [
    { key: 'patientName', header: 'Paciente', sortable: true, width: '18%' },
    { key: 'accessionNumber', header: 'Accession', sortable: true, width: '12%' },
    { key: 'studyDescription', header: 'Descripción', sortable: true, width: '20%' },
    { key: 'sourceAeTitle', header: 'Nodo', sortable: true, width: '10%' },
    { key: 'status', header: 'Estado', sortable: true, width: '12%' },
    { key: 'totalSizeBytes', header: 'Tamaño', sortable: true, width: '8%', align: 'right' },
    { key: 'seriesCount', header: 'Series/Inst.', width: '10%', align: 'center' },
    { key: 'createdAt', header: 'Recibido', sortable: true, width: '10%' },
  ];

  constructor() {
    this.facade.loadStudies();
  }

  protected onRowClick(study: Study): void {
    this.router.navigate(['/studies', study.id]);
  }
}
