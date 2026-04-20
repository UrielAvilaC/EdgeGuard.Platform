import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faCircleCheck,
  faCircleXmark,
  faClockRotateLeft,
  faSpinner,
  faPaperPlane,
  faDownload,
} from '@fortawesome/free-solid-svg-icons';

import { StudyStatus } from '../../models/study.models';

interface TimelineStep {
  status: StudyStatus;
  label: string;
  icon: import('@fortawesome/fontawesome-svg-core').IconDefinition;
  colorClass: string;
}

const TIMELINE_STEPS: TimelineStep[] = [
  { status: 'Receiving', label: 'Recibiendo', icon: faDownload, colorClass: 'text-sky-500' },
  { status: 'Completed', label: 'Completado', icon: faCircleCheck, colorClass: 'text-emerald-500' },
  { status: 'QueuedForSend', label: 'En cola PACS', icon: faClockRotateLeft, colorClass: 'text-amber-500' },
  { status: 'Sending', label: 'Enviando', icon: faSpinner, colorClass: 'text-violet-500' },
  { status: 'SentToPacs', label: 'Enviado a PACS', icon: faPaperPlane, colorClass: 'text-azure-600' },
];

const STATUS_ORDER: Record<StudyStatus, number> = {
  Receiving: 0,
  Completed: 1,
  QueuedForSend: 2,
  Sending: 3,
  SentToPacs: 4,
  Failed: -1,
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

  protected readonly isFailed = computed(() => this.currentStatus() === 'Failed');

  private readonly currentIndex = computed(() => STATUS_ORDER[this.currentStatus()] ?? -1);

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
