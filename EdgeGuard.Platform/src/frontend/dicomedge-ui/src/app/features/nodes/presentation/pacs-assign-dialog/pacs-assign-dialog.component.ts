import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { forkJoin, of } from 'rxjs';
import {
  faXmark,
  faServer,
  faLock,
  faCheckCircle,
  faTimesCircle,
  faMagnifyingGlass,
} from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { ToastService } from '../../../../core/services/toast.service';
import { PacsServer } from '../../../pacs/models/pacs.models';
import { PacsApiService } from '../../../pacs/infrastructure/pacs-api.service';
import { NodesApiService } from '../../infrastructure/nodes-api.service';
import { NodePacsAssignment } from '../../models/node.models';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

export interface PacsAssignDialogData {
  nodeId: string;
  nodeName: string;
  currentAssignments: NodePacsAssignment[];
}

interface PacsRowState {
  server: PacsServer;
  checked: boolean;
  initiallyChecked: boolean;
  inheritedFromHub: boolean;
  cEchoIntervalSeconds: number;
}

@Component({
  selector: 'app-pacs-assign-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiLoadingSpinner,
    UiEmptyState,
    UiChip,
    UiAlert,
    UiDialog,
  ],
  templateUrl: './pacs-assign-dialog.component.html',
  styleUrl: './pacs-assign-dialog.component.scss',
})
export class PacsAssignDialog implements OnInit {
  private readonly dialogRef = inject(MatDialogRef<PacsAssignDialog>);
  readonly data: PacsAssignDialogData = inject(MAT_DIALOG_DATA);
  private readonly pacsApi = inject(PacsApiService);
  private readonly nodesApi = inject(NodesApiService);
  private readonly toast = inject(ToastService);

  protected readonly faXmark = faXmark;
  protected readonly faServer = faServer;
  protected readonly faLock = faLock;
  protected readonly faCheckCircle = faCheckCircle;
  protected readonly faTimesCircle = faTimesCircle;
  protected readonly faMagnifyingGlass = faMagnifyingGlass;

  protected readonly rows = signal<PacsRowState[]>([]);
  protected readonly loadingServers = signal(true);
  protected readonly saving = signal(false);
  protected readonly searchQuery = signal('');
  protected readonly loadError = signal(false);

  protected readonly filteredRows = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.rows();
    return this.rows().filter(
      (r) =>
        r.server.name.toLowerCase().includes(q) ||
        r.server.aeTitle.toLowerCase().includes(q) ||
        r.server.hostName.toLowerCase().includes(q),
    );
  });

  protected readonly pendingChanges = computed(() => {
    const toAssign = this.rows().filter(
      (r) => !r.inheritedFromHub && r.checked && !r.initiallyChecked,
    ).length;
    const toUnassign = this.rows().filter(
      (r) => !r.inheritedFromHub && !r.checked && r.initiallyChecked,
    ).length;
    return toAssign + toUnassign;
  });

  ngOnInit(): void {
    this.loadPacsServers();
  }

  private loadPacsServers(): void {
    this.loadingServers.set(true);
    this.loadError.set(false);

    this.pacsApi.getPacsServers({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        const assignmentMap = new Map(
          this.data.currentAssignments.map((a) => [a.pacsId, a]),
        );

        this.rows.set(
          result.items.map((server) => {
            const assignment = assignmentMap.get(server.id);
            return {
              server,
              checked: !!assignment?.isActive,
              initiallyChecked: !!assignment?.isActive,
              inheritedFromHub: assignment?.inheritedFromHub ?? false,
              cEchoIntervalSeconds: assignment?.cEchoIntervalSeconds ?? 300,
            };
          }),
        );
        this.loadingServers.set(false);
      },
      error: () => {
        this.loadingServers.set(false);
        this.loadError.set(true);
      },
    });
  }

  protected toggleRow(row: PacsRowState): void {
    if (row.inheritedFromHub) return;
    this.rows.update((rows) =>
      rows.map((r) =>
        r.server.id === row.server.id ? { ...r, checked: !r.checked } : r,
      ),
    );
  }

  protected updateInterval(row: PacsRowState, value: number): void {
    this.rows.update((rows) =>
      rows.map((r) =>
        r.server.id === row.server.id
          ? { ...r, cEchoIntervalSeconds: Math.max(5, Math.min(86400, value)) }
          : r,
      ),
    );
  }

  protected onSave(): void {
    const toAssign = this.rows().filter(
      (r) => !r.inheritedFromHub && r.checked && !r.initiallyChecked,
    );
    const toUnassign = this.rows().filter(
      (r) => !r.inheritedFromHub && !r.checked && r.initiallyChecked,
    );

    if (toAssign.length === 0 && toUnassign.length === 0) {
      this.dialogRef.close(false);
      return;
    }

    this.saving.set(true);

    const assignCalls = toAssign.map((r) =>
      this.nodesApi.assignPacs(this.data.nodeId, r.server.id, {
        cEchoIntervalSeconds: r.cEchoIntervalSeconds,
      }),
    );
    const unassignCalls = toUnassign.map((r) =>
      this.nodesApi.unassignPacs(this.data.nodeId, r.server.id),
    );

    forkJoin([...assignCalls, ...unassignCalls, of(null)]).subscribe({
      next: () => {
        this.saving.set(false);
        const msg: string[] = [];
        if (toAssign.length) msg.push(`${toAssign.length} PACS asignado(s)`);
        if (toUnassign.length) msg.push(`${toUnassign.length} PACS desvinculado(s)`);
        this.toast.success(msg.join(' · '));
        this.dialogRef.close(true);
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Ocurrió un error al guardar los cambios');
      },
    });
  }

  protected onCancel(): void {
    this.dialogRef.close(false);
  }
}
