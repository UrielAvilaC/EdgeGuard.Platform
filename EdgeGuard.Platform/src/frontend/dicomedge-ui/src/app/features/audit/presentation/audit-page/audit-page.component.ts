import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faSync, faFilter, faXmark, faEye } from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar.component';
import { UiDropdown } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { TableColumn } from '../../../../shared/models/table.model';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { AuditLogDto, SEVERITY_OPTIONS, getSeverityColor, getSeverityLabel } from '../../models/audit.models';
import { AuditStore } from '../../services/audit.store';
import { AuditFacade } from '../../services/audit.facade';
import { AuditDetailDialog } from '../audit-detail-dialog/audit-detail-dialog.component';

@Component({
  selector: 'app-audit-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AuditStore, AuditFacade],
  imports: [
    FormsModule, FontAwesomeModule, UiPageHeader, UiButton, UiDataTable, UiCellDef,
    UiChip, UiSearchBar, UiDropdown, UiAlert, UiIconButton, RelativeTimePipe,
  ],
  templateUrl: './audit-page.component.html',
  styleUrl: './audit-page.component.scss'
})
export default class AuditPage {
  protected readonly facade = inject(AuditFacade);
  private readonly dialog = inject(MatDialog);

  protected readonly faSync = faSync;
  protected readonly faFilter = faFilter;
  protected readonly faXmark = faXmark;
  protected readonly faEye = faEye;

  protected readonly getSeverityColor = getSeverityColor;
  protected readonly getSeverityLabel = getSeverityLabel;

  protected readonly severityOptions = SEVERITY_OPTIONS.map(o => ({ value: o.value, label: o.label }));
  protected readonly severityFilter = signal<string>('');
  protected readonly eventTypeFilter = signal<string>('');

  protected readonly columns: TableColumn<AuditLogDto>[] = [
    { key: 'eventType', header: 'Evento', sortable: true },
    { key: 'action', header: 'Acción', sortable: true },
    { key: 'severity', header: 'Severidad', sortable: true, width: '120px' },
    { key: 'userName', header: 'Usuario', sortable: true },
    { key: 'isSuccess', header: 'Resultado', width: '90px' },
    { key: 'createdAt', header: 'Fecha', sortable: true, width: '140px' },
    { key: 'actions' as keyof AuditLogDto & string, header: '', width: '60px' },
  ];

  constructor() {
    this.facade.loadLogs();
    this.facade.loadEventTypes();
  }

  protected eventTypeOptions(): { value: string; label: string }[] {
    return this.facade.eventTypes().map(t => ({ value: t, label: t }));
  }

  protected onSeverityChange(value: string): void {
    this.severityFilter.set(value);
    this.facade.updateFilter({ severity: value || undefined });
  }

  protected onEventTypeChange(value: string): void {
    this.eventTypeFilter.set(value);
    this.facade.updateFilter({ eventType: value || undefined });
  }

  protected openDetail(log: AuditLogDto): void {
    this.dialog.open(AuditDetailDialog, { data: log, width: '640px', autoFocus: true, disableClose: true });
  }

  protected asString(value: unknown): string {
    return value as string;
  }
}
