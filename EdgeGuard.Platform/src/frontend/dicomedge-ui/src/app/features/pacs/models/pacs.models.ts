import { BaseFilter } from '../../../shared/models/filter.model';

export interface PacsServer {
  id: string;
  name: string;
  aeTitle: string;
  hostName: string;
  port: number;
  description: string | null;
  isEnabled: boolean;
  isGlobal: boolean;
  maxConcurrentAssociations: number;
  timeoutSeconds: number;
  lastCEchoAt: string | null;
  lastCEchoSuccess: boolean;
  isReachable: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface PacsServerFilter extends BaseFilter {
  isEnabled?: boolean;
  isGlobal?: boolean;
}

export interface CreatePacsServerRequest {
  name: string;
  aeTitle: string;
  hostName: string;
  port: number;
  description?: string;
  isGlobal?: boolean;
  maxConcurrentAssociations?: number;
  timeoutSeconds?: number;
}

export interface UpdatePacsServerRequest {
  name: string;
  hostName: string;
  port: number;
  description?: string;
  maxConcurrentAssociations?: number;
  timeoutSeconds?: number;
}
