import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faSync, faUpload, faUndo } from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiDropdown } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiIconButton } from '../../../../shared/components/ui-icon-button/ui-icon-button.component';
import { ToastService } from '../../../../core/services/toast.service';
import { NodesApiService } from '../../../nodes/infrastructure/nodes-api.service';
import { Node } from '../../../nodes/models/node.models';
import { NodeConfigApiService } from '../../infrastructure/node-config-api.service';
import { NodeConfigurationProfileDto } from '../../models/settings.models';

@Component({
  selector: 'app-node-config-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FontAwesomeModule, FormsModule, UiPageHeader, UiButton, UiDropdown, UiChip,
    UiAlert, UiIconButton,
  ],
  templateUrl: './node-config-page.component.html',
  styleUrl: './node-config-page.component.scss'
})
export default class NodeConfigPage {
  private readonly nodesApi = inject(NodesApiService);
  private readonly nodeConfigApi = inject(NodeConfigApiService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly faSync = faSync;
  protected readonly faUpload = faUpload;
  protected readonly faUndo = faUndo;

  protected readonly nodes = signal<Node[]>([]);
  protected readonly nodeOptions = signal<{ value: string; label: string }[]>([]);
  protected readonly selectedNodeId = signal<string>('');
  protected readonly configs = signal<NodeConfigurationProfileDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly categories = signal<string[]>([]);

  constructor() {
    this.nodesApi.getActive().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(nodes => {
      this.nodes.set(nodes);
      this.nodeOptions.set(nodes.map(n => ({ value: n.id, label: n.name })));
    });
  }

  protected onNodeSelect(nodeId: string): void {
    this.selectedNodeId.set(nodeId);
    this.loadNodeConfig();
  }

  protected loadNodeConfig(): void {
    const nodeId = this.selectedNodeId();
    if (!nodeId) return;
    this.loading.set(true);
    this.nodeConfigApi.getByNode(nodeId).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: configs => {
        this.configs.set(configs);
        const cats = [...new Set(configs.map(c => c.category))];
        this.categories.set(cats);
        this.loading.set(false);
        this.error.set(null);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Error al cargar la configuración del nodo');
      },
    });
  }

  protected configsByCategory(category: string): NodeConfigurationProfileDto[] {
    return this.configs().filter(c => c.category === category);
  }

  protected resetSetting(key: string): void {
    const nodeId = this.selectedNodeId();
    if (!nodeId) return;
    this.nodeConfigApi.resetSetting(nodeId, key).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.toast.success('Configuración restablecida');
        this.loadNodeConfig();
      },
      error: () => this.toast.error('Error al restablecer la configuración'),
    });
  }

  protected pushConfig(): void {
    const nodeId = this.selectedNodeId();
    if (!nodeId) return;
    this.nodeConfigApi.pushConfig(nodeId).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: result => {
        if (result.success) this.toast.success('Configuración enviada al nodo');
        else this.toast.error(result.message);
      },
      error: () => this.toast.error('Error al enviar la configuración'),
    });
  }
}
