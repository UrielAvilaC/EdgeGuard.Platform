export type DispatchStatus =
  | 'PendingValidation'
  | 'Validated'
  | 'Routed'
  | 'Queued'
  | 'Dispatching'
  | 'Delivered'
  | 'DeliveryFailed'
  | 'ValidationFailed';

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

export interface QueueMessage {
  id: string;
  messageType: string;
  triggerEvent: string | null;
  patientId: string | null;
  patientName: string | null;
  sendingFacility: string | null;
  status: string;
  dispatchStatus: DispatchStatus;
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

export interface QueuedMessage {
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

import { ChipColor } from '../../../shared/components/ui-chip/ui-chip.component';

export const DISPATCH_STATUS_OPTIONS: { value: DispatchStatus; label: string; color: ChipColor }[] = [
  { value: 'PendingValidation', label: 'Pendiente validación', color: 'default' },
  { value: 'Validated', label: 'Validado', color: 'info' },
  { value: 'Routed', label: 'Enrutado', color: 'info' },
  { value: 'Queued', label: 'En cola', color: 'warning' },
  { value: 'Dispatching', label: 'Despachando', color: 'warning' },
  { value: 'Delivered', label: 'Entregado', color: 'success' },
  { value: 'DeliveryFailed', label: 'Fallo entrega', color: 'danger' },
  { value: 'ValidationFailed', label: 'Fallo validación', color: 'danger' },
];
