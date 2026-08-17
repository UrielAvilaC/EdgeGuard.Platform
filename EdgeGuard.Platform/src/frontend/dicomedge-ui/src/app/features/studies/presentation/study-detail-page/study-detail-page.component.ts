import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
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
  faImage,
  faPaperPlane,
} from '@fortawesome/free-solid-svg-icons';

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiStatCard } from '../../../../shared/components/ui-stat-card/ui-stat-card.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { PersonNamePipe, cleanPersonName } from '../../../../shared/pipes/person-name.pipe';
import { Study, StudyInfrastructure, UpdateStudyRequest, STUDY_STATUS_OPTIONS } from '../../models/study.models';
import { StudiesStore } from '../../services/studies.store';
import { StudiesFacade } from '../../services/studies.facade';
import { StudiesApiService } from '../../infrastructure/studies-api.service';
import { StudyStatusTimeline } from '../study-status-timeline/study-status-timeline.component';
import { StudyReportPanel } from '../study-report-panel/study-report-panel.component';
import { StudyRequeueDialog, StudyRequeueDialogData } from '../study-requeue-dialog/study-requeue-dialog.component';

const STATUS_TRANSITION_OPTIONS: DropdownOption<string>[] = [
  { value: 'Completed', label: 'Completado' },
  { value: 'Failed', label: 'Fallido' },
  { value: 'Finalized', label: 'Finalizado' },
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
    UiStatCard,
    UiDropdown,
    UiInputText,
    UiSlideToggle,
    RelativeTimePipe,
    FileSizePipe,
    PersonNamePipe,
    StudyStatusTimeline,
    StudyReportPanel,
  ],
  templateUrl: './study-detail-page.component.html',
  styleUrl: './study-detail-page.component.scss'
})
export default class StudyDetailPage {
  protected readonly facade = inject(StudiesFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly studiesApi = inject(StudiesApiService);

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
  protected readonly faImage = faImage;
  protected readonly faPaperPlane = faPaperPlane;
  protected readonly statusTransitionOptions = STATUS_TRANSITION_OPTIONS;

  protected readonly study = this.facade.selectedStudy;
  protected readonly pageTitle = computed(() => {
    const s = this.study();
    return s ? `Estudio — ${cleanPersonName(s.patientName) || s.studyInstanceUid}` : 'Detalle de Estudio';
  });

  protected readonly editing = signal(false);
  protected editForm: UpdateStudyRequest = {};
  protected newStatus: string | null = null;

  /** Infrastructure view (node + PACS identity and connectivity) served by the Hub. */
  protected readonly infra = signal<StudyInfrastructure | null>(null);
  private loadedInfraId: string | null = null;

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.facade.loadStudyById(id);
    }
    this.facade.startRealtimeRefresh();

    // Load the Infraestructura card data (node name/status + PACS name/reachability)
    // from the Hub whenever the study resolves.
    effect(() => {
      const s = this.study();
      if (s && s.id !== this.loadedInfraId) {
        this.loadedInfraId = s.id;
        this.studiesApi.getInfrastructure(s.id).subscribe({
          next: (info) => this.infra.set(info),
          error: () => this.infra.set(null),
        });
      }
    });
  }

  /** Two-letter initials from the (cleaned) patient name for the hero avatar. */
  protected initials(name: string | null): string {
    const clean = cleanPersonName(name) || '';
    const parts = clean.split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '—';
    return (parts[0][0] + (parts[1]?.[0] ?? '')).toUpperCase();
  }

  /** Tailwind dot color for the origin node connectivity indicator. */
  protected readonly nodeDotClass = computed(() => {
    switch (this.infra()?.nodeStatus) {
      case 'Online': return 'bg-emerald-500';
      case 'Offline': return 'bg-red-500';
      case undefined:
      case null: return 'bg-gray-300 dark:bg-gray-600';
      default: return 'bg-amber-500';
    }
  });

  /** Tailwind dot color for the PACS destination, derived from the last C-ECHO result. */
  protected readonly pacsDotClass = computed(() => {
    const reachable = this.infra()?.pacsReachable;
    if (reachable === true) return 'bg-emerald-500';
    if (reachable === false) return 'bg-red-500';
    return 'bg-gray-300 dark:bg-gray-600';
  });

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

  protected openRequeueDialog(): void {
    const s = this.study();
    if (!s || !s.sourceNodeId) return;

    this.dialog.open(StudyRequeueDialog, {
      data: {
        studyId: s.id,
        sourceNodeId: s.sourceNodeId,
        sourceNodeName: s.sourceAeTitle ?? s.sourceNodeId,
      } satisfies StudyRequeueDialogData,
      autoFocus: false,
    }).afterClosed().subscribe((pacsIds: string[] | null) => {
      if (pacsIds && pacsIds.length > 0) {
        this.facade.requeue(s.id, pacsIds);
      }
    });
  }

  protected confirmStatusChange(): void {
    const s = this.study();
    if (!s || !this.newStatus) return;

    this.dialog.open(UiConfirmDialog, {
      data: {
        title: 'Cambiar estado del estudio',
        message: `¿Cambiar el estado de "${cleanPersonName(s.patientName) || 'este estudio'}" a "${this.newStatus}"?`,
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
