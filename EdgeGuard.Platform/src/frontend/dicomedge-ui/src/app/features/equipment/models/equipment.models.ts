export interface Modality {
  code: string;
  displayName: string;
  isSupported: boolean;
  isActive: boolean;
  sortOrder: number;
}

export interface NodeEquipment {
  id: string;
  nodeId: string;
  aeTitle: string;
  displayName: string | null;
  modalityCodes: string[];
  stationAeTitle: string | null;
  stationName: string | null;
  ipAddress: string | null;
  isEnabled: boolean;
  location: string | null;
  department: string | null;
  manufacturer: string | null;
  model: string | null;
  notes: string | null;
  lastConnectionAt: string | null;
  isOnline: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateNodeEquipmentRequest {
  aeTitle: string;
  displayName?: string;
  modalityCodes: string[];
  stationAeTitle?: string;
  stationName?: string;
  ipAddress?: string;
  location?: string;
  department?: string;
  manufacturer?: string;
  model?: string;
  notes?: string;
}

export type UpdateNodeEquipmentRequest = CreateNodeEquipmentRequest;
