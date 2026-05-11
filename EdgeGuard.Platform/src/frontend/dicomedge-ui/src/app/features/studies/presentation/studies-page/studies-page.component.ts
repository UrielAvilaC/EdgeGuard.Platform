import { ChangeDetectionStrategy, Component, inject, TemplateRef, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faFileExport,
  faExclamationTriangle,
  faSync,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { TruncatePipe } from '../../../../shared/pipes/truncate.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { Study } from '../../models/study.models';
import { StudiesStore } from '../../services/studies.store';
import { StudiesFacade } from '../../services/studies.facade';
import { StudyFilters } from '../study-filters/study-filters.component';

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
  templateUrl: './studies-page.component.html',
  styleUrl: './studies-page.component.scss'
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
