// ── Config Status ──

export interface WhatsAppConfigStatus {
  enabled: boolean;
  automaticDeliveryEnabled: boolean;
  provider: string;
  providerConfigured: boolean;
  defaultCountryPrefix: string;
  activeTemplatesCount: number;
  activeRulesCount: number;
  pendingNotifications: number;
  failedNotifications: number;
}

// ── Tags ──

export interface WhatsAppTemplateTag {
  tag: string;
  description: string;
  example: string | null;
}

// ── Templates ──

export interface WhatsAppTemplateVariable {
  position: number;
  tag: string;
}

export interface WhatsAppTemplate {
  id: string;
  name: string;
  contentSid: string;
  description: string | null;
  isActive: boolean;
  variables: WhatsAppTemplateVariable[];
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateWhatsAppTemplateRequest {
  name: string;
  contentSid: string;
  description?: string;
  tags: string[];
}

export interface UpdateWhatsAppTemplateRequest {
  name: string;
  contentSid: string;
  description?: string;
  tags: string[];
}

// ── Auto-Send Rules ──

export interface WhatsAppAutoSendRule {
  id: string;
  studyStatus: string;
  templateId: string;
  templateName: string | null;
  isEnabled: boolean;
  description: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateWhatsAppAutoSendRuleRequest {
  studyStatus: string;
  templateId: string;
  description?: string;
}

export interface UpdateWhatsAppAutoSendRuleRequest {
  templateId?: string;
  isEnabled?: boolean;
  description?: string;
}

// ── Notifications ──

export type WhatsAppNotificationStatus = 'Pending' | 'Sent' | 'Failed' | 'Skipped';
export type WhatsAppTriggerSource = 'Automatic' | 'Manual';

export interface WhatsAppNotification {
  id: string;
  studyId: string;
  patientId: string | null;
  phoneNumber: string;
  normalizedPhone: string | null;
  templateId: string | null;
  contentSid: string | null;
  studyStatus: string | null;
  status: WhatsAppNotificationStatus;
  triggeredBy: WhatsAppTriggerSource;
  providerName: string | null;
  providerMessageId: string | null;
  sentAt: string | null;
  attempts: number;
  lastError: string | null;
  createdAt: string;
}

// ── Manual Send ──

export interface WhatsAppRecipient {
  phoneNumber: string;
  name?: string;
}

export interface SendWhatsAppManualRequest {
  studyId: string;
  templateId: string;
  recipients: WhatsAppRecipient[];
}

export interface WhatsAppSendResult {
  phoneNumber: string;
  normalizedPhone: string | null;
  notificationId: string | null;
  success: boolean;
  error: string | null;
}

export interface SendWhatsAppManualResponse {
  notificationIds: string[];
  succeeded: number;
  failed: number;
  results: WhatsAppSendResult[];
}

// ── Notification status options for UI ──

export const NOTIFICATION_STATUS_OPTIONS: { value: WhatsAppNotificationStatus; label: string; color: string }[] = [
  { value: 'Pending', label: 'Pendiente', color: 'warning' },
  { value: 'Sent', label: 'Enviado', color: 'success' },
  { value: 'Failed', label: 'Fallido', color: 'error' },
  { value: 'Skipped', label: 'Omitido', color: 'default' },
];

export const STUDY_STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: 'Receiving', label: 'Recibiendo' },
  { value: 'Completed', label: 'Completado' },
  { value: 'QueuedForSend', label: 'En cola para envío' },
  { value: 'Sending', label: 'Enviando' },
  { value: 'SentToPacs', label: 'Enviado al PACS' },
  { value: 'Failed', label: 'Fallido' },
];
