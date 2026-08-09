import { Pipe, PipeTransform } from '@angular/core';

import { STUDY_STATUS_OPTIONS, StudyStatus } from '../models/study.models';

const STATUS_LABELS = new Map<string, string>(
  STUDY_STATUS_OPTIONS.map((o) => [o.value, o.label]),
);

/**
 * Maps a {@link StudyStatus} to its Spanish display label
 * (e.g. `WaitingForImageLinks` → "En espera de liga"). Falls back to the raw
 * value for any unknown status.
 */
@Pipe({ name: 'studyStatusLabel' })
export class StudyStatusLabelPipe implements PipeTransform {
  transform(status: StudyStatus | string | null | undefined): string {
    if (!status) return '';
    return STATUS_LABELS.get(status) ?? status;
  }
}
