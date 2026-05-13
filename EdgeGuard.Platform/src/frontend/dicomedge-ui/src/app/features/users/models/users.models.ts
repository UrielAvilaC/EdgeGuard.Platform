export interface UserDto {
  id: string;
  username: string;
  fullName: string;
  isActive: boolean;
  isLocked: boolean;
  lastLoginAt: string | null;
  createdAt: string;
  roles: string[];
  directPermissions: string[];
}

export interface UserFilter {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: string;
  search?: string;
  isActive?: boolean;
  role?: string;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  fullName: string;
  roles?: string[];
}

export interface UpdateProfileRequest {
  fullName: string;
}

export interface ResetPasswordRequest {
  newPassword: string;
}

export type Role =
  | 'Admin'
  | 'Operator'
  | 'Viewer'
  | 'Technician'
  | 'Auditor'
  | 'Manager'
  | 'ServiceAccount'
  | 'Support';

export const ROLES: { value: Role; label: string }[] = [
  { value: 'Admin', label: 'Administrador' },
  { value: 'Operator', label: 'Operador' },
  { value: 'Viewer', label: 'Visor' },
  { value: 'Technician', label: 'Técnico' },
  { value: 'Auditor', label: 'Auditor' },
  { value: 'Manager', label: 'Gerente' },
  { value: 'ServiceAccount', label: 'Cuenta de Servicio' },
  { value: 'Support', label: 'Soporte' },
];

export interface PermissionGroup {
  label: string;
  permissions: { value: string; label: string }[];
}

export const PERMISSION_GROUPS: PermissionGroup[] = [
  {
    label: 'Estudios',
    permissions: [
      { value: 'ViewStudies', label: 'Ver estudios' },
      { value: 'SendStudies', label: 'Enviar estudios' },
      { value: 'DeleteStudies', label: 'Eliminar estudios' },
      { value: 'ArchiveStudies', label: 'Archivar estudios' },
      { value: 'ExportStudies', label: 'Exportar estudios' },
      { value: 'EditStudyMetadata', label: 'Editar metadata' },
      { value: 'AnonymizeStudies', label: 'Anonimizar estudios' },
    ],
  },
  {
    label: 'Cola',
    permissions: [
      { value: 'ViewQueue', label: 'Ver cola' },
      { value: 'ManageQueue', label: 'Gestionar cola' },
      { value: 'RetryTransfers', label: 'Reintentar transferencias' },
      { value: 'CancelTransfers', label: 'Cancelar transferencias' },
    ],
  },
  {
    label: 'Configuración',
    permissions: [
      { value: 'ViewConfiguration', label: 'Ver configuración' },
      { value: 'EditConfiguration', label: 'Editar configuración' },
      { value: 'ManageModalities', label: 'Gestionar modalidades' },
      { value: 'ManageRoutingRules', label: 'Gestionar reglas de ruteo' },
      { value: 'ManageStorage', label: 'Gestionar almacenamiento' },
      { value: 'ManageNetwork', label: 'Gestionar red' },
    ],
  },
  {
    label: 'Monitoreo',
    permissions: [
      { value: 'ViewMetrics', label: 'Ver métricas' },
      { value: 'ViewAuditLogs', label: 'Ver auditoría' },
      { value: 'ViewSystemStatus', label: 'Ver estado del sistema' },
      { value: 'ExportReports', label: 'Exportar reportes' },
      { value: 'ManageAlerts', label: 'Gestionar alertas' },
    ],
  },
  {
    label: 'Usuarios',
    permissions: [
      { value: 'ViewUsers', label: 'Ver usuarios' },
      { value: 'ManageUsers', label: 'Gestionar usuarios' },
      { value: 'ManageRoles', label: 'Gestionar roles' },
      { value: 'GrantPermissions', label: 'Otorgar permisos' },
    ],
  },
  {
    label: 'Nodos',
    permissions: [
      { value: 'ViewNodes', label: 'Ver nodos' },
      { value: 'ManageEdgeNodes', label: 'Gestionar nodos' },
      { value: 'RestartNodes', label: 'Reiniciar nodos' },
      { value: 'UpdateNodes', label: 'Actualizar nodos' },
    ],
  },
  {
    label: 'Sistema',
    permissions: [
      { value: 'SystemBackup', label: 'Respaldo del sistema' },
      { value: 'SystemRestore', label: 'Restaurar sistema' },
      { value: 'ViewSystemLogs', label: 'Ver logs del sistema' },
      { value: 'SystemMaintenance', label: 'Mantenimiento' },
      { value: 'DatabaseOperations', label: 'Operaciones de BD' },
    ],
  },
];
