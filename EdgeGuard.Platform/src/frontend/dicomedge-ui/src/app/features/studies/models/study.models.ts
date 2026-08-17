import { BaseFilter } from '../../../shared/models/filter.model';

/** Clinical lifecycle of a study. The PACS transport lives on {@link StudyPacsStatus}. */
export type StudyStatus =
  | 'Scheduled'
  | 'Receiving'
  | 'Completed'
  | 'WaitingForImageLinks'
  | 'WaitingForReport'
  | 'Finalized';

/** Progress through the PACS-send pipeline, independent of the clinical status. */
export type StudyPacsStatus = 'NotQueued' | 'Queued' | 'Sending' | 'Sent' | 'Failed';

export type ReportFormat = 'None' | 'Html' | 'PlainText';

export interface StudyReport {
  studyId: string;
  status: string;
  reportFormat: ReportFormat;
  content: string | null;
  hasPdf: boolean;
  imageLinks: string[];
}

export type ModalityType = 'CT' | 'MR' | 'CR' | 'DX' | 'US' | 'NM' | 'PT';

export interface Study {
  id: string;
  studyInstanceUid: string;
  accessionNumber: string | null;
  studyDate: string | null;
  studyDescription: string | null;
  referringPhysician: string | null;
  /** DICOM Patient ID (MRN) as it arrived on the study. */
  patientId: string | null;
  /** Id of the linked patient record in the catalogue; null when the study is unlinked. */
  patientRecordId: string | null;
  patientName: string | null;
  sourceNodeId: string | null;
  sourceAeTitle: string | null;
  status: StudyStatus;
  pacsStatus: StudyPacsStatus;
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
  reportFormat: ReportFormat;
  hasReport: boolean;
  hasImageLinks: boolean;
  imageLinks: string[];
}

export interface StudyInfrastructure {
  sourceNodeId: string | null;
  sourceNodeName: string | null;
  sourceAeTitle: string | null;
  /** Node status enum name (Online/Offline/Degraded/…), or null when unknown. */
  nodeStatus: string | null;
  nodeLastHeartbeatAt: string | null;
  targetPacsId: string | null;
  targetPacsName: string | null;
  targetPacsAeTitle: string | null;
  /** Last known C-ECHO reachability of the PACS from this node; null when never checked. */
  pacsReachable: boolean | null;
  pacsLastEchoAt: string | null;
  sentToPacsAt: string | null;
  pacsSendAttempts: number;
  pacsSendLastError: string | null;
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
  { value: 'Scheduled', label: 'Agendado' },
  { value: 'Receiving', label: 'Recibiendo' },
  { value: 'Completed', label: 'Completado' },
  { value: 'WaitingForImageLinks', label: 'En espera de liga' },
  { value: 'WaitingForReport', label: 'En espera de reporte' },
  { value: 'Finalized', label: 'Finalizado' },
];

export const STUDY_PACS_STATUS_OPTIONS: { value: StudyPacsStatus; label: string }[] = [
  { value: 'NotQueued', label: 'Sin encolar' },
  { value: 'Queued', label: 'En cola PACS' },
  { value: 'Sending', label: 'Enviando' },
  { value: 'Sent', label: 'Enviado a PACS' },
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
