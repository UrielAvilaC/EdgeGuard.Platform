export interface AuditLogDto {
  id: string;
  eventType: string;
  action: string;
  severity: string;
  userId: string | null;
  userName: string | null;
  ipAddress: string | null;
  correlationId: string | null;
  entityId: string | null;
  entityType: string | null;
  isSuccess: boolean;
  errorMessage: string | null;
  details: string | null;
  createdAt: string;
}

export interface AuditLogFilter {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: string;
  eventType?: string;
  severity?: string;
  userId?: string;
  entityType?: string;
  entityId?: string;
  dateFrom?: string;
  dateTo?: string;
  isSuccess?: boolean;
  search?: string;
}

export type AuditSeverity = 'Information' | 'Warning' | 'Error' | 'Critical';

export const SEVERITY_OPTIONS: { value: AuditSeverity; label: string; color: 'info' | 'warning' | 'danger' | 'danger' }[] = [
  { value: 'Information', label: 'Información', color: 'info' },
  { value: 'Warning', label: 'Advertencia', color: 'warning' },
  { value: 'Error', label: 'Error', color: 'danger' },
  { value: 'Critical', label: 'Crítico', color: 'danger' },
];

export type ChipColor = 'default' | 'primary' | 'success' | 'warning' | 'danger' | 'info';

export function getSeverityColor(severity: string): ChipColor {
  switch (severity) {
    case 'Information': return 'info';
    case 'Warning': return 'warning';
    case 'Error': return 'danger';
    case 'Critical': return 'danger';
    default: return 'default';
  }
}

export function getSeverityLabel(severity: string): string {
  return SEVERITY_OPTIONS.find(o => o.value === severity)?.label ?? severity;
}
