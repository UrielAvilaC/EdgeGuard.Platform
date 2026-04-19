import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft,
  faPen,
  faSave,
  faXmark,
  faExclamationTriangle,
  faServer,
  faCalendar,
  faWeight,
  faLayerGroup,
  faUserDoctor,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header';
import { UiButton } from '../../../../shared/components/ui-button/ui-button';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown';
import { UiInputText } from '../../../../shared/forms/input-text/input-text';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { Study, UpdateStudyRequest, STUDY_STATUS_OPTIONS } from '../../models/study.models';
import { StudiesStore } from '../../services/studies.store';
import { StudiesFacade } from '../../services/studies.facade';
import { StudyStatusTimeline } from '../study-status-timeline/study-status-timeline';

const STATUS_TRANSITION_OPTIONS: DropdownOption<string>[] = [
  { value: 'Completed', label: 'Completado' },
  { value: 'Failed', label: 'Fallido' },
  { value: 'SentToPacs', label: 'Enviado a PACS' },
];

@Component({
  selector: 'app-study-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [StudiesStore, StudiesFacade],
  imports: [
    FormsModule,
    MatCardModule,
    FontAwesomeModule,
    UiPageHeader,
    UiButton,
    UiLoadingSpinner,
    UiAlert,
    UiStatusBadge,
    UiChip,
    UiDropdown,
    UiInputText,
    UiSlideToggle,
    RelativeTimePipe,
    FileSizePipe,
    StudyStatusTimeline,
  ],
  template: `
    <div class="space-y-6">
      <ui-page-header [title]="pageTitle()">
        <ui-button variant="ghost" size="sm" [icon]="faArrowLeft" (clicked)="goBack()">
          Volver a estudios
        </ui-button>
      </ui-page-header>

      @if (facade.selectedLoading()) {
        <ui-loading-spinner [overlay]="true" />
      }

      @if (facade.error(); as error) {
        <ui-alert type="error" [title]="error" />
      }

      @if (study(); as s) {
        <!-- Status Timeline -->
        <mat-card class="!p-6">
          <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
            Ciclo de Vida
          </h3>
          <app-study-status-timeline [currentStatus]="s.status" />
        </mat-card>

        <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
          <!-- Main Info -->
          <div class="xl:col-span-2 space-y-6">
            <mat-card class="!p-6">
              <div class="flex items-center justify-between mb-6">
                <h3 class="text-lg font-semibold text-gray-900 dark:text-white">
                  Información del Estudio
                </h3>
                @if (!editing()) {
                  <ui-button variant="ghost" size="sm" [icon]="faPen" (clicked)="startEditing()">
                    Editar
                  </ui-button>
                }
              </div>

              @if (editing()) {
                <div class="space-y-4">
                  <ui-input-text label="Descripción" [(ngModel)]="editForm.studyDescription" />
                  <ui-input-text label="Médico referente" [(ngModel)]="editForm.referringPhysician" />
                  <ui-input-text label="Accession Number" [(ngModel)]="editForm.accessionNumber" />
                  <div class="grid grid-cols-2 gap-4">
                    <ui-input-text label="Prioridad (0-10)" type="number" [(ngModel)]="editForm.priority" />
                    <div class="flex items-end pb-1">
                      <ui-slide-toggle label="Urgente" [(ngModel)]="editForm.isUrgent" />
                    </div>
                  </div>
                  <div class="flex gap-3 pt-2">
                    <ui-button size="sm" [icon]="faSave" (clicked)="saveEditing()">
                      Guardar
                    </ui-button>
                    <ui-button variant="ghost" size="sm" [icon]="faXmark" (clicked)="cancelEditing()">
                      Cancelar
                    </ui-button>
                  </div>
                </div>
              } @else {
                <dl class="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-4">
                  <div>
                    <dt class="text-sm text-gray-500 dark:text-gray-400">Study Instance UID</dt>
                    <dd class="mt-1 text-sm font-mono text-gray-900 dark:text-white break-all">{{ s.studyInstanceUid }}</dd>
                  </div>
                  <div>
                    <dt class="text-sm text-gray-500 dark:text-gray-400">Accession Number</dt>
                    <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ s.accessionNumber ?? '—' }}</dd>
                  </div>
                  <div>
                    <dt class="text-sm text-gray-500 dark:text-gray-400">Descripción</dt>
                    <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ s.studyDescription ?? '—' }}</dd>
                  </div>
                  <div>
                    <dt class="text-sm text-gray-500 dark:text-gray-400">Médico Referente</dt>
                    <dd class="mt-1 text-sm text-gray-900 dark:text-white">
                      <fa-icon [icon]="faUserDoctor" class="mr-1 text-gray-400" />
                      {{ s.referringPhysician ?? '—' }}
                    </dd>
                  </div>
                  <div>
                    <dt class="text-sm text-gray-500 dark:text-gray-400">Fecha de Estudio</dt>
                    <dd class="mt-1 text-sm text-gray-900 dark:text-white">
                      <fa-icon [icon]="faCalendar" class="mr-1 text-gray-400" />
                      {{ s.studyDate ?? '—' }}
                    </dd>
                  </div>
                  <div>
                    <dt class="text-sm text-gray-500 dark:text-gray-400">Prioridad</dt>
                    <dd class="mt-1 text-sm text-gray-900 dark:text-white flex items-center gap-2">
                      {{ s.priority }}
                      @if (s.isUrgent) {
                        <ui-chip color="danger">
                          <fa-icon [icon]="faExclamationTriangle" class="mr-1 text-xs" />
                          Urgente
                        </ui-chip>
                      }
                    </dd>
                  </div>
                </dl>
              }
            </mat-card>

            <!-- Paciente -->
            <mat-card class="!p-6">
              <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">Paciente</h3>
              <dl class="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-4">
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Nombre</dt>
                  <dd class="mt-1 text-sm font-medium text-gray-900 dark:text-white">{{ s.patientName ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Patient ID</dt>
                  <dd class="mt-1 text-sm font-mono text-gray-900 dark:text-white">{{ s.patientId ?? '—' }}</dd>
                </div>
              </dl>
            </mat-card>
          </div>

          <!-- Sidebar -->
          <div class="space-y-6">
            <!-- Estado -->
            <mat-card class="!p-6">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
                Estado Actual
              </h3>
              <div class="flex items-center gap-3 mb-4">
                <ui-status-badge [status]="s.status" />
              </div>

              <h4 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Cambiar estado</h4>
              <div class="flex gap-2">
                <ui-dropdown
                  class="flex-1"
                  placeholder="Seleccionar..."
                  [options]="statusTransitionOptions"
                  [(ngModel)]="newStatus"
                />
                <ui-button
                  size="sm"
                  [disabled]="!newStatus"
                  (clicked)="confirmStatusChange()"
                >
                  Aplicar
                </ui-button>
              </div>
            </mat-card>

            <!-- Métricas -->
            <mat-card class="!p-6">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
                Métricas
              </h3>
              <dl class="space-y-3">
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faLayerGroup" class="mr-1" /> Series
                  </dt>
                  <dd class="text-sm font-semibold text-gray-900 dark:text-white">{{ s.seriesCount }}</dd>
                </div>
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Instancias</dt>
                  <dd class="text-sm font-semibold text-gray-900 dark:text-white">{{ s.instanceCount }}</dd>
                </div>
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">
                    <fa-icon [icon]="faWeight" class="mr-1" /> Tamaño
                  </dt>
                  <dd class="text-sm font-semibold text-gray-900 dark:text-white">{{ s.totalSizeBytes | fileSize }}</dd>
                </div>
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Intentos PACS</dt>
                  <dd class="text-sm font-semibold text-gray-900 dark:text-white">{{ s.pacsSendAttempts }}</dd>
                </div>
              </dl>
            </mat-card>

            <!-- Nodo y PACS -->
            <mat-card class="!p-6">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
                <fa-icon [icon]="faServer" class="mr-1" /> Infraestructura
              </h3>
              <dl class="space-y-3">
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Nodo Origen</dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ s.sourceAeTitle ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">PACS Destino</dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ s.targetPacsId ?? '—' }}</dd>
                </div>
                <div>
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Enviado a PACS</dt>
                  <dd class="mt-1 text-sm text-gray-900 dark:text-white">{{ s.sentToPacsAt ? (s.sentToPacsAt | relativeTime) : '—' }}</dd>
                </div>
              </dl>
            </mat-card>

            <!-- Timestamps -->
            <mat-card class="!p-6">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-4">
                Timestamps
              </h3>
              <dl class="space-y-3">
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Creado</dt>
                  <dd class="text-sm text-gray-900 dark:text-white">{{ s.createdAt | relativeTime }}</dd>
                </div>
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Actualizado</dt>
                  <dd class="text-sm text-gray-900 dark:text-white">{{ s.updatedAt ? (s.updatedAt | relativeTime) : '—' }}</dd>
                </div>
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Primera imagen</dt>
                  <dd class="text-sm text-gray-900 dark:text-white">{{ s.firstImageReceivedAt ? (s.firstImageReceivedAt | relativeTime) : '—' }}</dd>
                </div>
                <div class="flex justify-between">
                  <dt class="text-sm text-gray-500 dark:text-gray-400">Última imagen</dt>
                  <dd class="text-sm text-gray-900 dark:text-white">{{ s.lastImageReceivedAt ? (s.lastImageReceivedAt | relativeTime) : '—' }}</dd>
                </div>
              </dl>
            </mat-card>
          </div>
        </div>
      }
    </div>
  `,
})
export default class StudyDetailPage {
  protected readonly facade = inject(StudiesFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  protected readonly faArrowLeft = faArrowLeft;
  protected readonly faPen = faPen;
  protected readonly faSave = faSave;
  protected readonly faXmark = faXmark;
  protected readonly faExclamationTriangle = faExclamationTriangle;
  protected readonly faServer = faServer;
  protected readonly faCalendar = faCalendar;
  protected readonly faWeight = faWeight;
  protected readonly faLayerGroup = faLayerGroup;
  protected readonly faUserDoctor = faUserDoctor;
  protected readonly statusTransitionOptions = STATUS_TRANSITION_OPTIONS;

  protected readonly study = this.facade.selectedStudy;
  protected readonly pageTitle = computed(() => {
    const s = this.study();
    return s ? `Estudio — ${s.patientName ?? s.studyInstanceUid}` : 'Detalle de Estudio';
  });

  protected readonly editing = signal(false);
  protected editForm: UpdateStudyRequest = {};
  protected newStatus: string | null = null;

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.facade.loadStudyById(id);
    }
  }

  protected goBack(): void {
    this.router.navigate(['/studies']);
  }

  protected startEditing(): void {
    const s = this.study();
    if (!s) return;
    this.editForm = {
      studyDescription: s.studyDescription ?? undefined,
      referringPhysician: s.referringPhysician ?? undefined,
      accessionNumber: s.accessionNumber ?? undefined,
      priority: s.priority,
      isUrgent: s.isUrgent,
    };
    this.editing.set(true);
  }

  protected cancelEditing(): void {
    this.editing.set(false);
  }

  protected saveEditing(): void {
    const s = this.study();
    if (!s) return;
    this.facade.updateStudy(s.id, this.editForm);
    this.editing.set(false);
  }

  protected confirmStatusChange(): void {
    const s = this.study();
    if (!s || !this.newStatus) return;

    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Cambiar estado del estudio',
        message: `¿Cambiar el estado de "${s.patientName ?? 'este estudio'}" a "${this.newStatus}"?`,
        confirmText: 'Cambiar',
        confirmColor: 'warn',
      } satisfies ConfirmDialogData,
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.facade.updateStatus(s.id, { status: this.newStatus! });
        this.newStatus = null;
      }
    });
  }
}
