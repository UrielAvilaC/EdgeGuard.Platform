export interface SystemSettingDto {
  key: string;
  value: string;
  category: string;
  displayName: string;
  valueType: string;
  description: string | null;
  isReadOnly: boolean;
}

export interface UpdateSettingRequest {
  value: string;
}

export interface NodeConfigurationProfileDto {
  nodeId: string;
  settingKey: string;
  value: string;
  category: string;
  displayName: string;
  valueType: string;
  isOverridden: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ConfigPushResultDto {
  success: boolean;
  message: string;
}

export interface BatchUpdateNodeSettingsRequest {
  settings: { key: string; value: string }[];
}

export interface BatchUpdateNodeSettingsResponse {
  updated: number;
  notFound: number;
  failedKeys: string[];
}

export const SETTING_CATEGORIES: { key: string; label: string; icon: string }[] = [
  { key: 'General', label: 'General', icon: 'cog' },
  { key: 'HL7', label: 'HL7', icon: 'chart-bar' },
  { key: 'Dispatch', label: 'Despacho', icon: 'paper-plane' },
  { key: 'Queue', label: 'Cola', icon: 'sync' },
  { key: 'WhatsApp', label: 'WhatsApp', icon: 'comments' },
  { key: 'Smtp', label: 'SMTP (Email)', icon: 'envelope' },
  { key: 'BackgroundJobs', label: 'Tareas en segundo plano', icon: 'clock' },
  { key: 'DataRetention', label: 'Retención de datos', icon: 'archive' },
];
