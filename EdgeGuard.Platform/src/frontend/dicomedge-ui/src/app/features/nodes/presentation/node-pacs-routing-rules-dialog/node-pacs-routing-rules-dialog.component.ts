import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
  computed,
} from '@angular/core';
import { MatDialogModule, MatDialogRef, MatDialog, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faXmark,
  faRoute,
  faPlus,
  faPen,
  faTrash,
  faToggleOn,
  faToggleOff,
  faArrowUp,
  faArrowDown,
  faCircleInfo,
} from '@fortawesome/free-solid-svg-icons';

import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import { ToastService } from '../../../../core/services/toast.service';
import { NodeDicomRoutingRulesApiService } from '../../infrastructure/node-dicom-routing-rules-api.service';
import { NodeDicomRoutingRule, CreateNodeDicomRoutingRuleRequest } from '../../models/node-dicom-routing-rule.models';
import {
  NodeDicomRuleFormDialog,
  NodeDicomRuleFormDialogData,
  NodeDicomRuleFormDialogResult,
} from '../node-dicom-rule-form-dialog/node-dicom-rule-form-dialog.component';
import { UiDialog } from '../../../../shared/components/ui-dialog/ui-dialog.component';

export interface NodePacsRoutingRulesDialogData {
  nodeId: string;
  nodeName: string;
  pacsName: string;
  pacsAeTitle: string;
}

@Component({
  selector: 'app-node-pacs-routing-rules-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatDialogModule,
    FontAwesomeModule,
    UiButton,
    UiIconButton,
    UiLoadingSpinner,
    UiEmptyState,
    UiAlert,
    UiChip,
    UiDialog,
  ],
  templateUrl: './node-pacs-routing-rules-dialog.component.html',
})
export class NodePacsRoutingRulesDialog implements OnInit {
  private readonly dialogRef = inject(MatDialogRef<NodePacsRoutingRulesDialog>);
  readonly data: NodePacsRoutingRulesDialogData = inject(MAT_DIALOG_DATA);
  private readonly api = inject(NodeDicomRoutingRulesApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  protected readonly faXmark = faXmark;
  protected readonly faRoute = faRoute;
  protected readonly faPlus = faPlus;
  protected readonly faPen = faPen;
  protected readonly faTrash = faTrash;
  protected readonly faToggleOn = faToggleOn;
  protected readonly faToggleOff = faToggleOff;
  protected readonly faArrowUp = faArrowUp;
  protected readonly faArrowDown = faArrowDown;
  protected readonly faCircleInfo = faCircleInfo;

  protected readonly rules = signal<NodeDicomRoutingRule[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly busyIds = signal<Set<string>>(new Set());

  protected readonly sortedRules = computed(() =>
    [...this.rules()].sort((a, b) => a.priority - b.priority),
  );

  ngOnInit(): void {
    this.loadRules();
  }

  private loadRules(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getByNode(this.data.nodeId).subscribe({
      next: (all) => {
        this.rules.set(
          all.filter(r => r.destinationAeTitle.toLowerCase() === this.data.pacsAeTitle.toLowerCase()),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar las reglas de ruteo.');
        this.loading.set(false);
      },
    });
  }

  protected openCreateDialog(): void {
    this.dialog
      .open(NodeDicomRuleFormDialog, {
        data: {
          destinationAeTitle: this.data.pacsAeTitle,
          nodeId: this.data.nodeId,
        } satisfies NodeDicomRuleFormDialogData,
        disableClose: true,
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((result: NodeDicomRuleFormDialogResult | null) => {
        if (!result) return;
        this.api.create(this.data.nodeId, result as CreateNodeDicomRoutingRuleRequest).subscribe({
          next: (rule) => {
            this.rules.update(r => [...r, rule]);
            this.toast.success('Regla creada correctamente.');
          },
          error: () => this.toast.error('Error al crear la regla.'),
        });
      });
  }

  protected openEditDialog(rule: NodeDicomRoutingRule): void {
    this.dialog
      .open(NodeDicomRuleFormDialog, {
        data: {
          rule,
          destinationAeTitle: this.data.pacsAeTitle,
          nodeId: this.data.nodeId,
        } satisfies NodeDicomRuleFormDialogData,
        disableClose: true,
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((result: NodeDicomRuleFormDialogResult | null) => {
        if (!result) return;
        this.markBusy(rule.id, true);
        this.api.update(this.data.nodeId, rule.id, result).subscribe({
          next: (updated) => {
            this.rules.update(rs => rs.map(r => r.id === updated.id ? updated : r));
            this.markBusy(rule.id, false);
            this.toast.success('Regla actualizada.');
          },
          error: () => {
            this.markBusy(rule.id, false);
            this.toast.error('Error al actualizar la regla.');
          },
        });
      });
  }

  protected toggleEnabled(rule: NodeDicomRoutingRule): void {
    this.markBusy(rule.id, true);
    const call = rule.isEnabled
      ? this.api.disable(this.data.nodeId, rule.id)
      : this.api.enable(this.data.nodeId, rule.id);

    call.subscribe({
      next: () => {
        this.rules.update(rs =>
          rs.map(r => r.id === rule.id ? { ...r, isEnabled: !r.isEnabled } : r),
        );
        this.markBusy(rule.id, false);
        this.toast.success(rule.isEnabled ? 'Regla deshabilitada.' : 'Regla habilitada.');
      },
      error: () => {
        this.markBusy(rule.id, false);
        this.toast.error('Error al cambiar el estado de la regla.');
      },
    });
  }

  protected confirmDelete(rule: NodeDicomRoutingRule): void {
    this.dialog
      .open(UiConfirmDialog, {
        data: {
          title: 'Eliminar regla',
          message: `¿Eliminar la regla "${rule.name}"? Esta acción no se puede deshacer.`,
          confirmText: 'Eliminar',
          confirmColor: 'warn',
        } satisfies ConfirmDialogData,
      })
      .afterClosed()
      .subscribe((confirmed: boolean) => {
        if (!confirmed) return;
        this.markBusy(rule.id, true);
        this.api.delete(this.data.nodeId, rule.id).subscribe({
          next: () => {
            this.rules.update(rs => rs.filter(r => r.id !== rule.id));
            this.markBusy(rule.id, false);
            this.toast.success('Regla eliminada.');
          },
          error: () => {
            this.markBusy(rule.id, false);
            this.toast.error('Error al eliminar la regla.');
          },
        });
      });
  }

  protected isBusy(id: string): boolean {
    return this.busyIds().has(id);
  }

  private markBusy(id: string, busy: boolean): void {
    this.busyIds.update(s => {
      const next = new Set(s);
      busy ? next.add(id) : next.delete(id);
      return next;
    });
  }

  protected onClose(): void {
    this.dialogRef.close();
  }
}
