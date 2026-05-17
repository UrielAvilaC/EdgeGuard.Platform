import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft,
  faCloudUploadAlt,
  faSave,
  faSync,
  faCodeBranch,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiEmptyState } from '../../../../shared/components/ui-empty-state/ui-empty-state.component';
import { ToastService } from '../../../../core/services/toast.service';
import { NodesApiService } from '../../infrastructure/nodes-api.service';
import { NodeConfigApiService } from '../../../settings/infrastructure/node-config-api.service';
import { NodeConfigurationProfileDto } from '../../../settings/models/settings.models';
import { NodeConfigCategoryPanel } from '../node-config-category-panel/node-config-category-panel.component';

const CATEGORY_LABELS: Record<string, string> = {
  general: 'General',
  hub: 'Conexión Hub',
  dicom: 'DICOM',
  cleanup: 'Limpieza',
  transfer: 'Transferencia',
  security: 'Seguridad',
  storage: 'Almacenamiento',
  sender: 'Envío PACS',
  cecho: 'C-ECHO',
  nodeapi: 'API del Nodo',
  system: 'Sistema',
};

@Component({
  selector: 'app-node-config-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiAlert,
    UiLoadingSpinner,
    UiEmptyState,
    NodeConfigCategoryPanel,
  ],
  templateUrl: './node-config-page.component.html',
})
export default class NodeConfigPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly nodesApi = inject(NodesApiService);
  private readonly nodeConfigApi = inject(NodeConfigApiService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly faArrowLeft = faArrowLeft;
  protected readonly faCloudUploadAlt = faCloudUploadAlt;
  protected readonly faSave = faSave;
  protected readonly faSync = faSync;
  protected readonly faCodeBranch = faCodeBranch;

  private readonly nodeId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly nodeName = signal<string>('');
  protected readonly configs = signal<NodeConfigurationProfileDto[]>([]);
  protected readonly configVersion = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly pushing = signal(false);
  protected readonly error = signal<string | null>(null);

  /** Cambios pendientes: settingKey → nuevo valor */
  protected readonly pendingChanges = signal<Map<string, string>>(new Map());

  protected readonly hasPendingChanges = computed(() => this.pendingChanges().size > 0);

  protected readonly categories = computed(() => [
    ...new Set(this.configs().map((c) => c.category)),
  ]);

  protected readonly pageTitle = computed(() =>
    this.nodeName() ? `Configuración — ${this.nodeName()}` : 'Configuración de Nodo',
  );

  constructor() {
    if (!this.nodeId) {
      this.router.navigate(['/nodes']);
      return;
    }
    this.loadNode();
    this.loadConfig();
  }

  protected categoryLabel(cat: string): string {
    return CATEGORY_LABELS[cat.toLowerCase()] ?? cat;
  }

  protected configsByCategory(category: string): NodeConfigurationProfileDto[] {
    return this.configs().filter((c) => c.category === category);
  }

  protected pendingKeysForCategory(category: string): Set<string> {
    const keys = new Set<string>();
    const pending = this.pendingChanges();
    for (const cfg of this.configs().filter((c) => c.category === category)) {
      if (pending.has(cfg.settingKey)) keys.add(cfg.settingKey);
    }
    return keys;
  }

  protected onSettingChanged(event: { key: string; value: string }): void {
    const current = new Map(this.pendingChanges());
    current.set(event.key, event.value);
    this.pendingChanges.set(current);
  }

  protected onSettingReset(key: string): void {
    this.nodeConfigApi
      .resetSetting(this.nodeId, key)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Remove from pending if it was there
          const current = new Map(this.pendingChanges());
          current.delete(key);
          this.pendingChanges.set(current);
          this.toast.success('Configuración restablecida');
          this.loadConfig();
        },
        error: () => this.toast.error('Error al restablecer la configuración'),
      });
  }

  protected saveChanges(): void {
    const pending = this.pendingChanges();
    if (!pending.size) return;

    const settings = Array.from(pending.entries()).map(([key, value]) => ({ key, value }));
    this.saving.set(true);

    this.nodeConfigApi
      .batchUpdate(this.nodeId, { settings })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          this.pendingChanges.set(new Map());
          if ((result.failedKeys?.length ?? 0) > 0) {
            this.toast.error(`Guardado parcial. Claves con error: ${result.failedKeys.join(', ')}`);
          } else if (result.notFound > 0) {
            this.toast.success(`Guardado: ${result.updated}. Claves no encontradas: ${result.notFound}.`);
          } else {
            this.toast.success(`${result.updated} configuración(es) guardadas`);
          }
          this.loadConfig();
        },
        error: () => {
          this.saving.set(false);
          this.toast.error('Error al guardar los cambios');
        },
      });
  }

  protected saveAndPush(): void {
    const pending = this.pendingChanges();
    if (!pending.size) {
      this.pushConfig();
      return;
    }

    const settings = Array.from(pending.entries()).map(([key, value]) => ({ key, value }));
    this.saving.set(true);

    this.nodeConfigApi
      .batchUpdate(this.nodeId, { settings })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          this.pendingChanges.set(new Map());

          if ((result.failedKeys?.length ?? 0) > 0) {
            this.toast.error(`Guardado parcial. Claves con error: ${result.failedKeys.join(', ')}. No se aplicará la configuración.`);
            this.loadConfig();
            return;
          }

          this.pushConfig();
        },
        error: () => {
          this.saving.set(false);
          this.toast.error('Error al guardar los cambios');
        },
      });
  }

  protected reload(): void {
    if (this.hasPendingChanges()) {
      this.pendingChanges.set(new Map());
    }
    this.loadConfig();
  }

  protected goBack(): void {
    this.router.navigate(['/nodes', this.nodeId]);
  }

  private loadNode(): void {
    this.nodesApi
      .getById(this.nodeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (node) => this.nodeName.set(node.name),
        error: () => {},
      });
  }

  private loadConfig(): void {
    this.loading.set(true);
    this.error.set(null);
    this.nodeConfigApi
      .getByNode(this.nodeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (configs) => {
          this.configs.set(configs);
          this.loading.set(false);
          this.loadVersion();
        },
        error: () => {
          this.loading.set(false);
          this.error.set('Error al cargar la configuración del nodo');
        },
      });
  }

  private loadVersion(): void {
    this.nodeConfigApi
      .getVersion(this.nodeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.configVersion.set(res.configVersion),
        error: () => {},
      });
  }

  private pushConfig(): void {
    this.pushing.set(true);
    this.nodeConfigApi
      .pushConfig(this.nodeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.pushing.set(false);
          // Guard against 204 No Content (result === null) — treat as success
          if (!result || result.success) {
            this.toast.success('Configuración aplicada al nodo');
          } else {
            this.toast.error(
              result.message
                ? `Configuración guardada. El nodo respondió: ${result.message}`
                : 'Configuración guardada en el servidor, pero no se pudo confirmar la aplicación al nodo.',
            );
          }
          this.loadConfig();
        },
        error: () => {
          this.pushing.set(false);
          // Config was already saved — push notification to node failed
          this.toast.error('Configuración guardada en el servidor. No se pudo notificar al nodo en este momento.');
          this.loadConfig();
        },
      });
  }
}
