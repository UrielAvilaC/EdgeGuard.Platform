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
  template: `
    @if (isFailed()) {
      <div class="flex items-center gap-3 p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
        <fa-icon [icon]="faCircleXmark" class="text-red-500 text-xl" />
        <div>
          <p class="font-semibold text-red-700 dark:text-red-400">Estado: Fallido</p>
          <p class="text-sm text-red-600 dark:text-red-300">El estudio falló durante el procesamiento.</p>
        </div>
      </div>
    } @else {
      <div class="flex items-center gap-0" role="list" aria-label="Timeline de estado del estudio">
        @for (step of steps; track step.status; let i = $index; let last = $last) {
          <div class="flex items-center" role="listitem"
               [attr.aria-current]="step.status === currentStatus() ? 'step' : null">
            <div class="flex flex-col items-center gap-1.5">
              <div class="w-10 h-10 rounded-full flex items-center justify-center transition-all"
                   [class]="getStepBgClass(step, i)">
                <fa-icon [icon]="step.icon" [class]="getStepIconClass(step, i)" />
              </div>
              <span class="text-xs font-medium whitespace-nowrap"
                    [class]="getStepLabelClass(step, i)">
                {{ step.label }}
              </span>
            </div>
            @if (!last) {
              <div class="w-12 h-0.5 mx-1 mt-[-18px] transition-colors"
                   [class]="getConnectorClass(i)">
              </div>
            }
          </div>
        }
      </div>
    }
  `,
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
