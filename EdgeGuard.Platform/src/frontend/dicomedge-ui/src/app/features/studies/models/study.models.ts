import { BaseFilter } from '../../../shared/models/filter.model';

export type StudyStatus =
  | 'Receiving'
  | 'Completed'
  | 'QueuedForSend'
  | 'Sending'
  | 'SentToPacs'
  | 'Failed';

export type ModalityType = 'CT' | 'MR' | 'CR' | 'DX' | 'US' | 'NM' | 'PT';

export interface Study {
  id: string;
  studyInstanceUid: string;
  accessionNumber: string | null;
  studyDate: string | null;
  studyDescription: string | null;
  referringPhysician: string | null;
  patientId: string | null;
  patientName: string | null;
  sourceNodeId: string | null;
  sourceAeTitle: string | null;
  status: StudyStatus;
  instanceCount: number;
  seriesCount: number;
  totalSizeBytes: number;
  firstImageReceivedAt: string | null;
  lastImageReceivedAt: string | null;
  priority: number;
  isUrgent: boolean;
  targetPacsId: string | null;
  sentToPacsAt: string | null;
  pacsSendAttempts: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface StudyFilter extends BaseFilter {
  status?: StudyStatus;
  sourceNodeId?: string;
  patientId?: string;
  modality?: ModalityType;
  dateFrom?: string;
  dateTo?: string;
  isUrgent?: boolean;
}

export interface UpdateStudyRequest {
  studyDescription?: string;
  referringPhysician?: string;
  accessionNumber?: string;
  priority?: number;
  isUrgent?: boolean;
}

export interface UpdateStudyStatusRequest {
  status: string;
  reason?: string;
}

export const STUDY_STATUS_OPTIONS: { value: StudyStatus; label: string }[] = [
  { value: 'Receiving', label: 'Recibiendo' },
  { value: 'Completed', label: 'Completado' },
  { value: 'QueuedForSend', label: 'En cola PACS' },
  { value: 'Sending', label: 'Enviando' },
  { value: 'SentToPacs', label: 'Enviado a PACS' },
  { value: 'Failed', label: 'Fallido' },
];

export const MODALITY_OPTIONS: { value: ModalityType; label: string }[] = [
  { value: 'CT', label: 'CT — Tomografía' },
  { value: 'MR', label: 'MR — Resonancia' },
  { value: 'CR', label: 'CR — Radiografía' },
  { value: 'DX', label: 'DX — Digital X-Ray' },
  { value: 'US', label: 'US — Ultrasonido' },
  { value: 'NM', label: 'NM — Medicina Nuclear' },
  { value: 'PT', label: 'PT — PET' },
];
