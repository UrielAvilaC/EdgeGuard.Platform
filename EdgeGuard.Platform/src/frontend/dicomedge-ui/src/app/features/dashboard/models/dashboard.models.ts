export interface DashboardSummary {
  totalStudies: number;
  totalPatients: number;
  totalNodes: number;
  activeNodes: number;
  pendingPacsStudies: number;
  failedStudies: number;
  queueSummary: QueueSummary;
  hl7Status: Hl7ListenerStatus | null;
  recentStudies: DashboardStudy[];
  nodes: DashboardNode[];
}

export interface QueueSummary {
  pendingValidation: number;
  validated: number;
  routed: number;
  queued: number;
  dispatching: number;
  delivered: number;
  deliveryFailed: number;
  validationFailed: number;
  totalInPipeline: number;
}

export interface Hl7ListenerStatus {
  isRunning: boolean;
  port: number;
  activeConnections: number;
}

export interface DashboardStudy {
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

/** Clinical axis only — the PACS transport is reported separately in `pacsStatus`. */
export type StudyStatus =
  | 'Scheduled'
  | 'Receiving'
  | 'Completed'
  | 'WaitingForImageLinks'
  | 'WaitingForReport'
  | 'Finalized';

export interface DashboardNode {
  id: string;
  name: string;
  aeTitle: string;
  ipAddress: string;
  port: number;
  apiEndpoint: string | null;
  location: string | null;
  facilityName: string | null;
  status: NodeStatus;
  isEnabled: boolean;
  lastHeartbeatAt: string | null;
  healthCheckIntervalSeconds: number;
  /** Cuota administrada desde el Hub. null = sin límite, no se pinta barra. */
  storageLimitMb: number | null;
  /** Medición del nodo. null = nunca reportó. */
  storageDicomMb: number | null;
  storageDatabaseMb: number | null;
  storageVolumeFreeMb: number | null;
  storageVolumeTotalMb: number | null;
  storageMeasuredAt: string | null;
  totalStudiesReceived: number;
  totalStudiesSent: number;
  errorsLast24Hours: number;
  createdAt: string;
  updatedAt: string | null;
}

export type NodeStatus =
  | 'Online'
  | 'Offline'
  | 'Degraded'
  | 'Maintenance'
  | 'Starting'
  | 'Stopping';
