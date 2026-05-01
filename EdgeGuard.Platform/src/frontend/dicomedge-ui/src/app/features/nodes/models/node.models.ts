import { BaseFilter } from '../../../shared/models/filter.model';

export type NodeStatus =
  | 'Online'
  | 'Offline'
  | 'Degraded'
  | 'Maintenance'
  | 'Starting'
  | 'Stopping';

export interface NodePacsAssignment {
  pacsId: string;
  isActive: boolean;
  inheritedFromHub: boolean;
}

export interface Node {
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
  maxStorageMb: number;
  availableStorageMb: number;
  totalStudiesReceived: number;
  totalStudiesSent: number;
  errorsLast24Hours: number;
  createdAt: string;
  updatedAt: string | null;
  pacsAssignments: NodePacsAssignment[];
}

export interface NodeFilter extends BaseFilter {
  status?: NodeStatus;
  isEnabled?: boolean;
}

export interface CreateNodeRequest {
  name: string;
  aeTitle: string;
  ipAddress: string;
  port: number;
  apiEndpoint?: string;
  location?: string;
  facilityName?: string;
  healthCheckIntervalSeconds?: number;
}

export interface UpdateNodeRequest {
  location?: string;
  facilityName?: string;
  timeZone?: string;
  healthCheckIntervalSeconds?: number;
  maxStorageMb?: number;
}

export const NODE_STATUS_OPTIONS: { value: NodeStatus; label: string }[] = [
  { value: 'Online', label: 'En línea' },
  { value: 'Offline', label: 'Fuera de línea' },
  { value: 'Degraded', label: 'Degradado' },
  { value: 'Maintenance', label: 'Mantenimiento' },
  { value: 'Starting', label: 'Iniciando' },
  { value: 'Stopping', label: 'Deteniendo' },
];
