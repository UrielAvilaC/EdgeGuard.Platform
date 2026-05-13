export interface LoginRequest {
  username: string;
  password: string;
}

export interface RefreshRequest {
  accessToken: string;
  refreshToken: string;
}

export interface RevokeRequest {
  refreshToken: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
  permissions: string[];
}

export interface AuthErrorResponse {
  failureReason: string;
  lockoutEnd?: string;
}

export interface UserProfile {
  userId: string;
  userName: string;
  fullName: string;
  roles: string[];
  permissions: string[];
}

export type Permission =
  | 'ViewStudies' | 'SendStudies' | 'DeleteStudies' | 'ArchiveStudies' | 'ExportStudies' | 'EditStudyMetadata' | 'AnonymizeStudies'
  | 'ViewQueue' | 'ManageQueue' | 'RetryTransfers' | 'CancelTransfers'
  | 'ViewConfiguration' | 'EditConfiguration' | 'ManageModalities' | 'ManageRoutingRules' | 'ManageStorage' | 'ManageNetwork'
  | 'ViewMetrics' | 'ViewAuditLogs' | 'ViewSystemStatus' | 'ExportReports' | 'ManageAlerts'
  | 'ViewUsers' | 'ManageUsers' | 'ManageRoles' | 'GrantPermissions'
  | 'ViewNodes' | 'ManageEdgeNodes' | 'RestartNodes' | 'UpdateNodes'
  | 'SystemBackup' | 'SystemRestore' | 'ViewSystemLogs' | 'SystemMaintenance' | 'DatabaseOperations'
  | 'ViewWorklists' | 'ManageWorklists' | 'CompleteWorklistItems'
  | 'ViewSecurityEvents' | 'ManageSecurityPolicies' | 'SecurityAudit' | 'ManageEncryption' | 'ExportComplianceReports'
  | 'ApiAccess' | 'ManageApiKeys' | 'ManageIntegrations';

export type Role = 'Admin' | 'Operator' | 'Viewer' | 'Technician' | 'Auditor' | 'Manager' | 'ServiceAccount' | 'Support';

export interface StoredAuth {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
  user: UserProfile;
}
