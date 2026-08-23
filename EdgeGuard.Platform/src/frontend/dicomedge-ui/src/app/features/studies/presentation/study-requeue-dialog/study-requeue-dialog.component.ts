import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { forkJoin } from 'rxjs';
import { faXmark, faPaperPlane, faServer } from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { NodesApiService } from '../../../nodes/infrastructure/nodes-api.service';
import { PacsApiService } from '../../../pacs/infrastructure/pacs-api.service';
import { PacsServer } from '../../../pacs/models/pacs.models';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

export interface StudyRequeueDialogData {
  studyId: string;
  sourceNodeId: string;
  sourceNodeName: string;
}

interface PacsRowState {
  server: PacsServer;
  checked: boolean;
}

@Component({
  selector: 'app-study-requeue-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiLoadingSpinner,
    UiEmptyState,
    UiAlert,
    UiDialog,
  ],
  templateUrl: './study-requeue-dialog.component.html',
  styleUrl: './study-requeue-dialog.component.scss',
})
export class StudyRequeueDialog implements OnInit {
  private readonly dialogRef = inject(MatDialogRef<StudyRequeueDialog>);
  readonly data: StudyRequeueDialogData = inject(MAT_DIALOG_DATA);
  private readonly nodesApi = inject(NodesApiService);
  private readonly pacsApi = inject(PacsApiService);

  protected readonly faXmark = faXmark;
  protected readonly faPaperPlane = faPaperPlane;
  protected readonly faServer = faServer;

  protected readonly rows = signal<PacsRowState[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);

  ngOnInit(): void {
    this.loadAssignedPacs();
  }

  private loadAssignedPacs(): void {
    this.loading.set(true);
    this.loadError.set(false);

    forkJoin([
      this.nodesApi.getById(this.data.sourceNodeId),
      this.pacsApi.getPacsServers({ page: 1, pageSize: 100 }),
    ]).subscribe({
      next: ([node, pacsResult]) => {
        const activeIds = new Set(
          node.pacsAssignments.filter((a) => a.isActive).map((a) => a.pacsId),
        );

        this.rows.set(
          pacsResult.items
            .filter((server) => activeIds.has(server.id))
            .map((server) => ({ server, checked: true })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      },
    });
  }

  protected toggleRow(row: PacsRowState): void {
    this.rows.update((rows) =>
      rows.map((r) => (r.server.id === row.server.id ? { ...r, checked: !r.checked } : r)),
    );
  }

  protected get selectedCount(): number {
    return this.rows().filter((r) => r.checked).length;
  }

  protected onResend(): void {
    const pacsIds = this.rows().filter((r) => r.checked).map((r) => r.server.id);
    if (pacsIds.length === 0) return;
    this.dialogRef.close(pacsIds);
  }

  protected onCancel(): void {
    this.dialogRef.close(null);
  }
}
