import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft,
  faCog,
  faPen,
  faServer,
  faNetworkWired,
  faLocationDot,
  faHospital,
  faClock,
  faToggleOn,
  faToggleOff,
  faPlug,
  faTrash,
  faCheckCircle,
  faTimesCircle,
  faLock,
  faCircleQuestion,
  faWifi,
  faExclamationTriangle,
  faInfoCircle,
} from '@fortawesome/free-solid-svg-icons';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { DecimalPipe, SlicePipe } from '@angular/common';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiDataTable, UiCellDef } from '../../../../shared/components/ui-data-table/ui-data-table.component';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { TableColumn } from '../../../../shared/models/table.model';
import { UpdateNodeRequest, NodePacsCEchoStatus, PacsCEchoDestination } from '../../models/node.models';
import { NodesStore } from '../../services/nodes.store';
import { NodesFacade } from '../../services/nodes.facade';
import { NodeHealthCard } from '../node-health-card/node-health-card.component';
import { NodeFormDialog, NodeFormDialogData } from '../node-form-dialog/node-form-dialog.component';

import { StudiesApiService } from '../../../studies/infrastructure/studies-api.service';
import { Study } from '../../../studies/models/study.models';
import { PacsApiService } from '../../../pacs/infrastructure/pacs-api.service';
import { PacsServer } from '../../../pacs/models/pacs.models';
import { PacsAssignDialog, PacsAssignDialogData } from '../pacs-assign-dialog/pacs-assign-dialog.component';
import { NodesApiService } from '../../infrastructure/nodes-api.service';

@Component({
  selector: 'app-node-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [NodesStore, NodesFacade],
  imports: [
    RouterLink,
    MatCardModule,
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiLoadingSpinner,
    UiAlert,
    UiStatusBadge,
    UiChip,
    UiDataTable,
    UiCellDef,
    UiEmptyState,
    RelativeTimePipe,
    FileSizePipe,
    NodeHealthCard,
    DecimalPipe,
    SlicePipe,
  ],
  templateUrl: './node-detail-page.component.html',
  styleUrl: './node-detail-page.component.scss'
})
export default class NodeDetailPage {
  protected readonly facade = inject(NodesFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly studiesApi = inject(StudiesApiService);
  private readonly pacsApi = inject(PacsApiService);

  protected readonly faArrowLeft = faArrowLeft;
  protected readonly faCog = faCog;
  protected readonly faPen = faPen;
  protected readonly faServer = faServer;
  protected readonly faNetworkWired = faNetworkWired;
  protected readonly faLocationDot = faLocationDot;
  protected readonly faHospital = faHospital;
  protected readonly faClock = faClock;
  protected readonly faToggleOn = faToggleOn;
  protected readonly faToggleOff = faToggleOff;
  protected readonly faPlug = faPlug;
  protected readonly faTrash = faTrash;
  protected readonly faCheckCircle = faCheckCircle;
  protected readonly faTimesCircle = faTimesCircle;
  protected readonly faLock = faLock;
  protected readonly faCircleQuestion = faCircleQuestion;
  protected readonly faWifi = faWifi;
  protected readonly faExclamationTriangle = faExclamationTriangle;
  protected readonly faInfoCircle = faInfoCircle;

  protected readonly nodeStudies = signal<Study[]>([]);
  protected readonly studiesLoading = signal(false);
  protected readonly allPacsServers = signal<PacsServer[]>([]);
  /** Latest PACS C-ECHO status reported by the node to the Hub. */
  protected readonly pacsEchoStatus = signal<NodePacsCEchoStatus | null>(null);
  protected readonly pacsEchoLoading = signal(false);

  protected readonly pacsMap = computed(() =>
    new Map(this.allPacsServers().map((p) => [p.id, p])),
  );

  /** Maps AeTitle (lowercase) → PacsCEchoDestination for O(1) lookup in template. */
  protected readonly echoMap = computed(() => {
    const status = this.pacsEchoStatus();
    if (!status) return new Map<string, PacsCEchoDestination>();
    return new Map(status.destinations.map(d => [d.aeTitle.toLowerCase(), d]));
  });

  protected readonly echoSummary = computed(() => {
    const status = this.pacsEchoStatus();
    if (!status) return null;
    return { total: status.totalChecked, reachable: status.totalReachable, reportedAt: status.reportedAtUtc };
  });

  protected readonly pageTitle = computed(() => {
    const n = this.facade.selectedNode();
    return n ? `Nodo — ${n.name}` : 'Detalle de Nodo';
  });

  protected readonly studyColumns: TableColumn<Study>[] = [
    { key: 'patientName', header: 'Paciente', width: '25%' },
    { key: 'studyDescription', header: 'Descripción', width: '25%' },
    { key: 'status', header: 'Estado', width: '15%' },
    { key: 'totalSizeBytes', header: 'Tamaño', width: '12%', align: 'right' },
    { key: 'createdAt', header: 'Fecha', width: '15%' },
  ];

  private readonly nodeId = this.route.snapshot.paramMap.get('id');

  private readonly api = inject(NodesApiService);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    if (this.nodeId) {
      this.facade.loadNodeById(this.nodeId);
      this.loadNodeStudies(this.nodeId);
      this.loadPacsEchoStatus(this.nodeId);
    }
    this.loadAllPacsServers();
  }

  protected goBack(): void {
    this.router.navigate(['/nodes']);
  }

  protected goToStudy(study: Study): void {
    this.router.navigate(['/studies', study.id]);
  }

  protected openEditDialog(): void {
    const node = this.facade.selectedNode();
    if (!node) return;

    this.dialog.open(NodeFormDialog, {
      data: { node } satisfies NodeFormDialogData,
    }).afterClosed().subscribe((result: UpdateNodeRequest | null) => {
      if (result) {
        this.facade.updateNode(node.id, result);
      }
    });
  }

  protected confirmEnable(): void {
    const node = this.facade.selectedNode();
    if (!node) return;

    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Habilitar nodo',
        message: `¿Habilitar el nodo "${node.name}"?`,
        confirmText: 'Habilitar',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.facade.enableNode(node.id);
      }
    });
  }

  protected confirmDisable(): void {
    const node = this.facade.selectedNode();
    if (!node) return;

    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Deshabilitar nodo',
        message: `¿Deshabilitar el nodo "${node.name}"? Dejará de recibir estudios.`,
        confirmText: 'Deshabilitar',
        confirmColor: 'warn',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.facade.disableNode(node.id);
      }
    });
  }

  private loadNodeStudies(nodeId: string): void {
    this.studiesLoading.set(true);
    this.studiesApi.getByNode(nodeId).subscribe({
      next: (studies) => {
        this.nodeStudies.set(studies);
        this.studiesLoading.set(false);
      },
      error: () => this.studiesLoading.set(false),
    });
  }

  private loadAllPacsServers(): void {
    this.pacsApi.getPacsServers({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.allPacsServers.set(result.items),
      error: () => {},
    });
  }

  protected loadPacsEchoStatus(nodeId?: string): void {
    const id = nodeId ?? this.nodeId;
    if (!id) return;
    this.pacsEchoLoading.set(true);
    this.api.getPacsEchoStatus(id).pipe(
      catchError(() => of(null)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(status => {
      this.pacsEchoStatus.set(status);
      this.pacsEchoLoading.set(false);
    });
  }

  protected openPacsAssignDialog(): void {
    const node = this.facade.selectedNode();
    if (!node) return;

    this.dialog
      .open(PacsAssignDialog, {
        data: {
          nodeId: node.id,
          nodeName: node.name,
          currentAssignments: node.pacsAssignments,
        } satisfies PacsAssignDialogData,
        autoFocus: false,
        disableClose: true,
      })
      .afterClosed()
      .subscribe((changed: boolean) => {
        if (changed) {
          this.facade.loadNodeById(node.id);
        }
      });
  }

  protected confirmUnassignPacs(pacsId: string): void {
    const node = this.facade.selectedNode();
    if (!node) return;

    const pacsName = this.pacsMap().get(pacsId)?.name ?? pacsId;
    this.dialog
      .open(UiConfirmDialog, {
        data: {
          title: 'Desvincular PACS',
          message: `¿Desvincular el servidor PACS "${pacsName}" del nodo "${node.name}"?`,
          confirmText: 'Desvincular',
          confirmColor: 'warn',
        } satisfies ConfirmDialogData,
      })
      .afterClosed()
      .subscribe((confirmed: boolean) => {
        if (confirmed) {
          this.facade.unassignPacs(node.id, pacsId);
        }
      });
  }
}
