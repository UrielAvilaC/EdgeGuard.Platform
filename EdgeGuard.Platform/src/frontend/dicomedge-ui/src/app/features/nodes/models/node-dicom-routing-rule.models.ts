export interface NodeDicomRoutingRule {
  id: string;
  nodeId: string;
  name: string;
  priority: number;
  isEnabled: boolean;
  matchModality: string | null;
  matchSourceAeTitle: string | null;
  matchInstitution: string | null;
  matchStudyDesc: string | null;
  minInstanceCount: number | null;
  maxInstanceCount: number | null;
  destinationAeTitle: string;
  sendToPacs: boolean;
  sendToHub: boolean;
  anonymizeBeforeSend: boolean;
  matchCount: number;
  lastMatchedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateNodeDicomRoutingRuleRequest {
  name: string;
  priority: number;
  matchModality?: string;
  matchSourceAeTitle?: string;
  matchInstitution?: string;
  matchStudyDescContains?: string;
  minInstanceCount?: number;
  maxInstanceCount?: number;
  destinationAeTitle: string;
  sendToPacs: boolean;
  sendToHub: boolean;
  anonymizeBeforeSending: boolean;
}

export type UpdateNodeDicomRoutingRuleRequest = CreateNodeDicomRoutingRuleRequest;

export const DICOM_MODALITY_OPTIONS: { value: string; label: string }[] = [
  { value: 'CT', label: 'CT — Tomografía Computarizada' },
  { value: 'MR', label: 'MR — Resonancia Magnética' },
  { value: 'US', label: 'US — Ultrasonido' },
  { value: 'CR', label: 'CR — Radiografía Computarizada' },
  { value: 'DX', label: 'DX — Radiografía Digital' },
  { value: 'MG', label: 'MG — Mamografía' },
  { value: 'NM', label: 'NM — Medicina Nuclear' },
  { value: 'PT', label: 'PT — Tomografía PET' },
  { value: 'XA', label: 'XA — Angiografía' },
  { value: 'RF', label: 'RF — Fluoroscopía' },
  { value: 'OT', label: 'OT — Otro' },
];
