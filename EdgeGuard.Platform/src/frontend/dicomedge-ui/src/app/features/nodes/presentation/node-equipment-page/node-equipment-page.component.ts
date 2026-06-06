import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft,
  faPlus,
  faSync,
  faPen,
  faTrash,
  faToggleOn,
  faToggleOff,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import { ToastService } from '../../../../core/services/toast.service';
import { NodesApiService } from '../../infrastructure/nodes-api.service';
import { EquipmentApiService } from '../../../equipment/infrastructure/equipment-api.service';
import {
  Modality,
  NodeEquipment,
  CreateNodeEquipmentRequest,
} from '../../../equipment/models/equipment.models';
import {
  EquipmentFormDialog,
  EquipmentFormDialogData,
  EquipmentFormDialogResult,
} from '../../../equipment/presentation/equipment-form-dialog/equipment-form-dialog.component';

@Component({
  selector: 'app-node-equipment-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiIconButton,
    UiAlert,
    UiLoadingSpinner,
    UiEmptyState,
    UiChip,
  ],
  templateUrl: './node-equipment-page.component.html',
})
export default class NodeEquipmentPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly nodesApi = inject(NodesApiService);
  private readonly api = inject(EquipmentApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  protected readonly faArrowLeft = faArrowLeft;
  protected readonly faPlus = faPlus;
  protected readonly faSync = faSync;
  protected readonly faPen = faPen;
  protected readonly faTrash = faTrash;
  protected readonly faToggleOn = faToggleOn;
  protected readonly faToggleOff = faToggleOff;

  private readonly nodeId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly nodeName = signal<string>('');
  protected readonly equipment = signal<NodeEquipment[]>([]);
  protected readonly modalities = signal<Modality[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly busyIds = signal<Set<string>>(new Set());

  protected readonly pageTitle = computed(() =>
    this.nodeName() ? `Equipos — ${this.nodeName()}` : 'Equipos del Nodo',
  );

  constructor() {
    if (!this.nodeId) {
      this.router.navigate(['/nodes']);
      return;
    }
    this.loadNode();
    this.api.getModalities(true).subscribe({
      next: (m) => this.modalities.set(m),
      error: () => {},
    });
    this.load();
  }

  protected goBack(): void {
    this.router.navigate(['/nodes', this.nodeId]);
  }

  protected reload(): void {
    this.load();
  }

  private loadNode(): void {
    this.nodesApi.getById(this.nodeId).subscribe({
      next: (node) => this.nodeName.set(node.name),
      error: () => {},
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getByNode(this.nodeId).subscribe({
      next: (items) => {
        this.equipment.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar los equipos del nodo.');
        this.loading.set(false);
      },
    });
  }

  protected openCreateDialog(): void {
    this.dialog
      .open(EquipmentFormDialog, {
        data: {
          nodeId: this.nodeId,
          modalities: this.modalities(),
        } satisfies EquipmentFormDialogData,
        disableClose: true,
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((result: EquipmentFormDialogResult | null) => {
        if (!result) return;
        this.api.create(this.nodeId, result as CreateNodeEquipmentRequest).subscribe({
          next: (created) => {
            this.equipment.update((e) => [...e, created]);
            this.toast.success('Equipo creado correctamente.');
          },
          error: (err) => this.toast.error(this.extractError(err) ?? 'Error al crear el equipo.'),
        });
      });
  }

  protected openEditDialog(item: NodeEquipment): void {
    this.dialog
      .open(EquipmentFormDialog, {
        data: {
          equipment: item,
          nodeId: this.nodeId,
          modalities: this.modalities(),
        } satisfies EquipmentFormDialogData,
        disableClose: true,
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((result: EquipmentFormDialogResult | null) => {
        if (!result) return;
        this.markBusy(item.id, true);
        this.api.update(this.nodeId, item.id, result).subscribe({
          next: (updated) => {
            this.equipment.update((es) => es.map((e) => (e.id === updated.id ? updated : e)));
            this.markBusy(item.id, false);
            this.toast.success('Equipo actualizado.');
          },
          error: (err) => {
            this.markBusy(item.id, false);
            this.toast.error(this.extractError(err) ?? 'Error al actualizar el equipo.');
          },
        });
      });
  }

  protected toggleEnabled(item: NodeEquipment): void {
    this.markBusy(item.id, true);
    const call = item.isEnabled
      ? this.api.disable(this.nodeId, item.id)
      : this.api.enable(this.nodeId, item.id);

    call.subscribe({
      next: () => {
        this.equipment.update((es) =>
          es.map((e) => (e.id === item.id ? { ...e, isEnabled: !e.isEnabled } : e)),
        );
        this.markBusy(item.id, false);
        this.toast.success(item.isEnabled ? 'Equipo deshabilitado.' : 'Equipo habilitado.');
      },
      error: () => {
        this.markBusy(item.id, false);
        this.toast.error('Error al cambiar el estado del equipo.');
      },
    });
  }

  protected confirmDelete(item: NodeEquipment): void {
    this.dialog
      .open(UiConfirmDialog, {
        data: {
          title: 'Eliminar equipo',
          message: `¿Eliminar el equipo "${item.displayName ?? item.aeTitle}"? Esta acción no se puede deshacer.`,
          confirmText: 'Eliminar',
          confirmColor: 'warn',
        } satisfies ConfirmDialogData,
      })
      .afterClosed()
      .subscribe((confirmed: boolean) => {
        if (!confirmed) return;
        this.markBusy(item.id, true);
        this.api.delete(this.nodeId, item.id).subscribe({
          next: () => {
            this.equipment.update((es) => es.filter((e) => e.id !== item.id));
            this.markBusy(item.id, false);
            this.toast.success('Equipo eliminado.');
          },
          error: () => {
            this.markBusy(item.id, false);
            this.toast.error('Error al eliminar el equipo.');
          },
        });
      });
  }

  protected isBusy(id: string): boolean {
    return this.busyIds().has(id);
  }

  private markBusy(id: string, busy: boolean): void {
    this.busyIds.update((s) => {
      const next = new Set(s);
      busy ? next.add(id) : next.delete(id);
      return next;
    });
  }

  private extractError(err: unknown): string | null {
    const e = err as { error?: { error?: string; codes?: string[] } };
    return e?.error?.error ?? null;
  }
}
