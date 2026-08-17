import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faCircleCheck,
  faCircleXmark,
  faHourglassHalf,
  faFlagCheckered,
  faDownload,
  faCalendarAlt,
} from '@fortawesome/free-solid-svg-icons';

import { StudyStatus } from '../../models/study.models';

interface TimelineStep {
  label: string;
  icon: import('@fortawesome/fontawesome-svg-core').IconDefinition;
  colorClass: string;
}

// Clinical axis: after "Completado" the study progresses toward "Finalizado"
// (which the backend only assigns when both the image link and the report are present).
// PACS-send phases are intentionally not part of this axis — they are shown in the
// "Infraestructura" card of the study detail page.
const TIMELINE_STEPS: TimelineStep[] = [
  { label: 'Agendado', icon: faCalendarAlt, colorClass: 'text-indigo-500' },
  { label: 'Recibiendo', icon: faDownload, colorClass: 'text-sky-500' },
  { label: 'Completado', icon: faCircleCheck, colorClass: 'text-emerald-500' },
  { label: 'En espera de resultados', icon: faHourglassHalf, colorClass: 'text-amber-500' },
  { label: 'Finalizado', icon: faFlagCheckered, colorClass: 'text-teal-600' },
];

const STATUS_ORDER: Record<StudyStatus, number> = {
  Scheduled: 0,
  Receiving: 1,
  Completed: 2,
  // Awaiting one of the two clinical artifacts (link / report).
  WaitingForImageLinks: 3,
  WaitingForReport: 3,
  Finalized: 4,
};

@Component({
  selector: 'app-study-status-timeline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FontAwesomeModule],
  templateUrl: './study-status-timeline.component.html',
  styleUrl: './study-status-timeline.component.scss'
})
export class StudyStatusTimeline {
  readonly currentStatus = input.required<StudyStatus>();

  protected readonly faCircleXmark = faCircleXmark;
  protected readonly steps = TIMELINE_STEPS;

  /**
   * A PACS-send failure no longer lands on the clinical status, so the caller passes it in
   * explicitly. The timeline still renders its failed state, it just no longer infers it.
   */
  readonly pacsFailed = input(false);

  protected readonly isFailed = computed(() => this.pacsFailed());

  private readonly currentIndex = computed(() => STATUS_ORDER[this.currentStatus()] ?? -1);

  protected isCurrent(index: number): boolean {
    return index === this.currentIndex();
  }

  protected getStepBgClass(step: TimelineStep, index: number): string {
    const current = this.currentIndex();
    if (index < current) return 'bg-emerald-100 dark:bg-emerald-900/30';
    if (index === current) return `bg-current-step ${step.colorClass.replace('text-', 'bg-').replace('500', '100')} dark:bg-gray-700`;
    return 'bg-gray-100 dark:bg-gray-700';
  }

  protected getStepIconClass(step: TimelineStep, index: number): string {
    const current = this.currentIndex();
    if (index < current) return 'text-emerald-600 dark:text-emerald-400';
    if (index === current) return step.colorClass;
    return 'text-gray-400 dark:text-gray-500';
  }

  protected getStepLabelClass(step: TimelineStep, index: number): string {
    const current = this.currentIndex();
    if (index <= current) return 'text-gray-900 dark:text-white';
    return 'text-gray-400 dark:text-gray-500';
  }

  protected getConnectorClass(index: number): string {
    return index < this.currentIndex()
      ? 'bg-emerald-400 dark:bg-emerald-600'
      : 'bg-gray-200 dark:bg-gray-600';
  }
}
