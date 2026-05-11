// ── HL7 Listener ──

export interface Hl7ListenerStatus {
  isRunning: boolean;
  port: number;
  activeConnections: number;
}

// ── HL7 Messages ──

export type Hl7MessageStatus = 'Received' | 'Processing' | 'Processed' | 'Failed';

export type Hl7DispatchStatus =
  | 'PendingValidation'
  | 'Validated'
  | 'ValidationFailed'
  | 'Routed'
  | 'Queued'
  | 'Dispatching'
  | 'Delivered'
  | 'DeliveryFailed';

export interface Hl7MessageSummary {
  id: string;
  messageType: string;
  sendingApplication: string | null;
  sendingFacility: string | null;
  receivedAt: string;
  clientEndpoint: string | null;
  status: Hl7MessageStatus;
  processedAt: string | null;
  errorMessage: string | null;
}

export interface Hl7MessageDetail extends Hl7MessageSummary {
  content: string;
}

export interface Hl7Message {
  id: string;
  messageType: string;
  triggerEvent: string | null;
  patientId: string | null;
  patientName: string | null;
  sendingFacility: string | null;
  status: Hl7MessageStatus;
  dispatchStatus: Hl7DispatchStatus;
  targetNodeId: string | null;
  targetNodeName: string | null;
  priority: number;
  dispatchAttempts: number;
  dispatchError: string | null;
  receivedAt: string;
  validatedAt: string | null;
  routedAt: string | null;
  queuedAt: string | null;
  dispatchedAt: string | null;
  deliveredAt: string | null;
}

export interface Hl7MessageQueued {
  id: string;
  messageType: string;
  triggerEvent: string | null;
  patientId: string | null;
  targetNodeId: string | null;
  targetNodeName: string | null;
  priority: number;
  receivedAt: string;
  queuedAt: string | null;
}

// ── Queue Summary ──

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

// ── Routing Rules ──

export interface RoutingRule {
  id: string;
  name: string;
  priority: number;
  isEnabled: boolean;
  matchMessageType: string | null;
  matchTriggerEvent: string | null;
  matchSendingFacility: string | null;
  matchSendingApplication: string | null;
  targetNodeId: string;
  matchCount: number;
  lastMatchedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface RoutingRuleFilter {
  search?: string;
  isEnabled?: boolean;
  targetNodeId?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: string;
}

export interface CreateRoutingRuleRequest {
  name: string;
  targetNodeId: string;
  priority: number;
  matchMessageType?: string;
  matchTriggerEvent?: string;
  matchSendingFacility?: string;
  matchSendingApplication?: string;
}

export interface UpdateRoutingRuleRequest {
  name: string;
  targetNodeId: string;
  priority: number;
  matchMessageType?: string;
  matchTriggerEvent?: string;
  matchSendingFacility?: string;
  matchSendingApplication?: string;
}

export interface UpdatePriorityRequest {
  priority: number;
}

// ── Dispatch status options for UI ──

import { ChipColor } from '../../../shared/components/ui-chip/ui-chip.component';

export const DISPATCH_STATUS_OPTIONS: { value: Hl7DispatchStatus; label: string; color: ChipColor }[] = [
  { value: 'PendingValidation', label: 'Pendiente validación', color: 'default' },
  { value: 'Validated', label: 'Validado', color: 'info' },
  { value: 'ValidationFailed', label: 'Validación fallida', color: 'danger' },
  { value: 'Routed', label: 'Enrutado', color: 'info' },
  { value: 'Queued', label: 'En cola', color: 'warning' },
  { value: 'Dispatching', label: 'Despachando', color: 'warning' },
  { value: 'Delivered', label: 'Entregado', color: 'success' },
  { value: 'DeliveryFailed', label: 'Entrega fallida', color: 'danger' },
];

export const MESSAGE_STATUS_OPTIONS: { value: Hl7MessageStatus; label: string; color: ChipColor }[] = [
  { value: 'Received', label: 'Recibido', color: 'default' },
  { value: 'Processing', label: 'Procesando', color: 'warning' },
  { value: 'Processed', label: 'Procesado', color: 'success' },
  { value: 'Failed', label: 'Fallido', color: 'danger' },
];
