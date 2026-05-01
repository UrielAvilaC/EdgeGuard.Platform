import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faPlus,
  faSync,
  faPen,
  faToggleOn,
  faToggleOff,
  faTrash,
  faWifi,
  faFilter,
  faXmark,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiSearchBar } from '../../../../shared/components/ui-search-bar/ui-search-bar.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { PacsServer, CreatePacsServerRequest, UpdatePacsServerRequest, PacsServerFilter } from '../../models/pacs.models';
import { PacsStore } from '../../services/pacs.store';
import { PacsFacade } from '../../services/pacs.facade';
import { PacsFormDialog, PacsFormDialogData } from '../pacs-form-dialog/pacs-form-dialog.component';

@Component({
  selector: 'app-pacs-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PacsStore, PacsFacade],
  imports: [
    FormsModule,
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiIconButton,
    UiDataTable,
    UiCellDef,
    UiChip,
    UiAlert,
    UiSearchBar,
    UiSlideToggle,
    RelativeTimePipe,
  ],
  templateUrl: './pacs-page.component.html',
  styleUrl: './pacs-page.component.scss'
})
export default class PacsPage {
  protected readonly facade = inject(PacsFacade);
  private readonly dialog = inject(MatDialog);

  protected readonly faPlus = faPlus;
  protected readonly faSync = faSync;
  protected readonly faPen = faPen;
  protected readonly faToggleOn = faToggleOn;
  protected readonly faToggleOff = faToggleOff;
  protected readonly faTrash = faTrash;
  protected readonly faWifi = faWifi;
  protected readonly faFilter = faFilter;
  protected readonly faXmark = faXmark;

  protected filtersExpanded = false;

  protected readonly columns: TableColumn<PacsServer>[] = [
    { key: 'name', header: 'Nombre', sortable: true, width: '18%' },
    { key: 'aeTitle', header: 'AE Title', sortable: true, width: '12%' },
    { key: 'hostName', header: 'Host:Puerto', sortable: true, width: '16%' },
    { key: 'isReachable', header: 'Conectividad', width: '14%' },
    { key: 'lastCEchoAt', header: 'Último C-ECHO', sortable: true, width: '14%' },
    { key: 'maxConcurrentAssociations', header: 'Max Asoc.', width: '8%', align: 'center' },
    { key: 'actions', header: '', width: '12%' },
  ];

  constructor() {
    this.facade.loadServers();
  }

  protected onSearchChange(search: string): void {
    this.facade.updateFilter({ search: search || undefined });
  }

  protected onEnabledChange(isEnabled: boolean): void {
    this.facade.updateFilter({ isEnabled: isEnabled || undefined });
  }

  protected onGlobalChange(isGlobal: boolean): void {
    this.facade.updateFilter({ isGlobal: isGlobal || undefined });
  }

  protected openCreateDialog(): void {
    this.dialog.open(PacsFormDialog, {
      data: {} satisfies PacsFormDialogData,
    }).afterClosed().subscribe((result: CreatePacsServerRequest | null) => {
      if (result) {
        this.facade.createServer(result);
      }
    });
  }

  protected openEditDialog(server: PacsServer): void {
    this.dialog.open(PacsFormDialog, {
      data: { server } satisfies PacsFormDialogData,
    }).afterClosed().subscribe((result: UpdatePacsServerRequest | null) => {
      if (result) {
        this.facade.updateServer(server.id, result);
      }
    });
  }

  protected confirmDelete(server: PacsServer): void {
    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Eliminar servidor PACS',
        message: `¿Eliminar el servidor "${server.name}"? Esta acción no se puede deshacer.`,
        confirmText: 'Eliminar',
        confirmColor: 'warn',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.facade.deleteServer(server.id);
      }
    });
  }
}
