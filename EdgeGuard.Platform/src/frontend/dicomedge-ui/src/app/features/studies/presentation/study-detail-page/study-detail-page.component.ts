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

import { UiPageHeader } from '../../../../shared/components/ui-page-header/ui-page-header.component';
import { UiButton } from '../../../../shared/components/ui-button/ui-button.component';
import { UiLoadingSpinner } from '../../../../shared/components/ui-loading-spinner/ui-loading-spinner.component';
import { UiAlert } from '../../../../shared/components/ui-alert/ui-alert.component';
import { UiStatusBadge } from '../../../../shared/components/ui-status-badge/ui-status-badge.component';
import { UiChip } from '../../../../shared/components/ui-chip/ui-chip.component';
import { UiConfirmDialog, ConfirmDialogData } from '../../../../shared/components/ui-confirm-dialog/ui-confirm-dialog.component';
import { UiDropdown, DropdownOption } from '../../../../shared/forms/dropdown/dropdown.component';
import { UiInputText } from '../../../../shared/forms/input-text/input-text.component';
import { UiSlideToggle } from '../../../../shared/forms/slide-toggle/slide-toggle.component';
import { RelativeTimePipe } from '../../../../shared/pipes/relative-time.pipe';
import { FileSizePipe } from '../../../../shared/pipes/file-size.pipe';
import { Study, UpdateStudyRequest, STUDY_STATUS_OPTIONS } from '../../models/study.models';
import { StudiesStore } from '../../services/studies.store';
import { StudiesFacade } from '../../services/studies.facade';
import { StudyStatusTimeline } from '../study-status-timeline/study-status-timeline.component';
import { StudyReportPanel } from '../study-report-panel/study-report-panel.component';

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
