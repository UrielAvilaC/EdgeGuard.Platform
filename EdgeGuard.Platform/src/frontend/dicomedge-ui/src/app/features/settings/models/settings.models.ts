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

/**
 * Respuesta de `POST /node-configuration/{nodeId}/push`.
 *
 * No trae bandera de éxito: el Hub responde 400 cuando el push no llega al nodo, así que
 * todo lo que aparece aquí ya es un push aceptado. La interfaz declaraba antes
 * `{ success, message }`, campos que el backend nunca envió — `success` llegaba siempre
 * `undefined` y la pantalla daba por fallido hasta el push que sí funcionó.
 */
export interface ConfigPushResultDto {
  /** Versión que el nodo confirmó haber aplicado. */
  appliedVersion: string | null;
  /** Ajustes que el nodo escribió. */
  updatedCount: number;
}

export interface BatchUpdateNodeSettingsRequest {
  settings: { key: string; value: string }[];
}

export interface BatchUpdateNodeSettingsResponse {
  updated: number;
  notFound: number;
  /** Claves que este nodo no tiene en su perfil. No se perdió nada escrito. */
  notFoundKeys: string[];
  /** Claves que existían y aun así no pudieron guardarse. Esto sí es un fallo. */
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
