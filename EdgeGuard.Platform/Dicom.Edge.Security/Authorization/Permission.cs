namespace Dicom.Edge.Security.Authorization
{
    /// <summary>
    /// Defines granular permissions for operations in the EdgeGuard system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Permissions are organized by functional area and follow the principle of least privilege.
    /// Multiple permissions can be assigned to a role or user.
    /// </para>
    /// </remarks>
    public enum Permission
    {
        // ==================== Study Operations (1-9) ====================
        /// <summary>View DICOM studies.</summary>
        ViewStudies = 1,

        /// <summary>Send/transfer studies to other systems.</summary>
        SendStudies = 2,

        /// <summary>Delete studies from storage.</summary>
        DeleteStudies = 3,

        /// <summary>Archive studies to long-term storage.</summary>
        ArchiveStudies = 4,

        /// <summary>Export studies in various formats.</summary>
        ExportStudies = 5,

        /// <summary>Edit study metadata.</summary>
        EditStudyMetadata = 6,

        /// <summary>Anonymize study data.</summary>
        AnonymizeStudies = 7,

        // ==================== Queue Operations (10-19) ====================
        /// <summary>View transfer queue status.</summary>
        ViewQueue = 10,

        /// <summary>Manage queue items (remove, reprioritize).</summary>
        ManageQueue = 11,

        /// <summary>Retry failed transfers.</summary>
        RetryTransfers = 12,

        /// <summary>Cancel pending transfers.</summary>
        CancelTransfers = 13,

        // ==================== Configuration (20-29) ====================
        /// <summary>View system configuration.</summary>
        ViewConfiguration = 20,

        /// <summary>Edit system configuration.</summary>
        EditConfiguration = 21,

        /// <summary>Manage modality configurations.</summary>
        ManageModalities = 22,

        /// <summary>Manage routing rules.</summary>
        ManageRoutingRules = 23,

        /// <summary>Configure storage settings.</summary>
        ManageStorage = 24,

        /// <summary>Manage network settings.</summary>
        ManageNetwork = 25,

        // ==================== Monitoring & Metrics (30-39) ====================
        /// <summary>View performance metrics and dashboards.</summary>
        ViewMetrics = 30,

        /// <summary>View audit logs.</summary>
        ViewAuditLogs = 31,

        /// <summary>View system health status.</summary>
        ViewSystemStatus = 32,

        /// <summary>Export reports.</summary>
        ExportReports = 33,

        /// <summary>Configure alerting rules.</summary>
        ManageAlerts = 34,

        // ==================== User Management (40-49) ====================
        /// <summary>View users and roles.</summary>
        ViewUsers = 40,

        /// <summary>Create, edit, and delete users.</summary>
        ManageUsers = 41,

        /// <summary>Assign roles to users.</summary>
        ManageRoles = 42,

        /// <summary>Grant specific permissions to users.</summary>
        GrantPermissions = 43,

        // ==================== Node Management (50-59) ====================
        /// <summary>View Edge Node information.</summary>
        ViewNodes = 50,

        /// <summary>Register and configure Edge Nodes.</summary>
        ManageEdgeNodes = 51,

        /// <summary>Restart or stop Edge Nodes.</summary>
        RestartNodes = 52,

        /// <summary>Update Edge Node software.</summary>
        UpdateNodes = 53,

        // ==================== System Administration (60-69) ====================
        /// <summary>Perform system backups.</summary>
        SystemBackup = 60,

        /// <summary>Restore from backup.</summary>
        SystemRestore = 61,

        /// <summary>View system logs.</summary>
        ViewSystemLogs = 62,

        /// <summary>Perform system maintenance.</summary>
        SystemMaintenance = 63,

        /// <summary>Execute database operations.</summary>
        DatabaseOperations = 64,

        // ==================== Worklist & Orders (70-79) ====================
        /// <summary>View worklists.</summary>
        ViewWorklists = 70,

        /// <summary>Create and edit worklist items.</summary>
        ManageWorklists = 71,

        /// <summary>Complete worklist items.</summary>
        CompleteWorklistItems = 72,

        // ==================== Security & Compliance (80-89) ====================
        /// <summary>View security events.</summary>
        ViewSecurityEvents = 80,

        /// <summary>Manage security policies.</summary>
        ManageSecurityPolicies = 81,

        /// <summary>Perform security audits.</summary>
        SecurityAudit = 82,

        /// <summary>Manage encryption keys.</summary>
        ManageEncryption = 83,

        /// <summary>Export compliance reports.</summary>
        ExportComplianceReports = 84,

        // ==================== API & Integration (90-99) ====================
        /// <summary>Access API endpoints.</summary>
        ApiAccess = 90,

        /// <summary>Manage API keys.</summary>
        ManageApiKeys = 91,

        /// <summary>Configure external integrations.</summary>
        ManageIntegrations = 92
    }
}
