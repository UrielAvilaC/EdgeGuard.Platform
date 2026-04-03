namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// All keys for the node_settings table, grouped by category using nested static classes.
/// Every key maps to exactly one row. Always reference these constants instead of raw strings.
/// </summary>
public static class NodeSettingKeys
{
    // ── General ──────────────────────────────────────────────────────────────────
    public static class General
    {
        public const string NodeName     = "node.name";
        public const string AeTitle      = "node.ae_title";
        public const string Description  = "node.description";
        public const string Location     = "node.location";
        public const string FacilityName = "node.facility_name";
        public const string Timezone     = "node.timezone";
        public const string Version      = "node.version";      // read_only
        public const string ContactEmail = "node.contact_email";
        public const string ContactPhone = "node.contact_phone";
    }

    // ── Hub ───────────────────────────────────────────────────────────────────────
    public static class Hub
    {
        public const string Enabled               = "hub.enabled";
        public const string Protocol              = "hub.protocol";
        public const string Hostname              = "hub.hostname";
        public const string Port                  = "hub.port";
        public const string BasePath              = "hub.base_path";
        public const string ApiKey                = "hub.api_key";
        public const string TimeoutSeconds        = "hub.timeout_seconds";
        public const string HeartbeatIntervalSec  = "hub.heartbeat_interval_sec";
        public const string RegisterOnStartup     = "hub.register_on_startup";
        public const string PullConfigOnStartup   = "hub.pull_config_on_startup";
        public const string PullConfigIntervalMin = "hub.pull_config_interval_min";
        public const string TlsVerifyCertificate  = "hub.tls_verify_certificate";
        public const string MaxReconnectAttempts  = "hub.max_reconnect_attempts";
        public const string ReconnectDelaySeconds = "hub.reconnect_delay_seconds";
    }

    // ── DICOM ─────────────────────────────────────────────────────────────────────
    public static class Dicom
    {
        public const string ValidateCallingAe         = "dicom.validate_calling_ae";
        public const string AllowedAeTitles           = "dicom.allowed_ae_titles";             // json array
        public const string MaxAssociations           = "dicom.max_associations";
        public const string Port                      = "dicom.port";
        public const string AeTitle                   = "dicom.ae_title";
        public const string StudyCompletionTimeoutSec = "dicom.study_completion_timeout_sec";  // inactivity window
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────────
    public static class Cleanup
    {
        public const string Enabled            = "cleanup.enabled";
        public const string RetainDays         = "cleanup.retain_days";
        public const string RetainSentDays     = "cleanup.retain_sent_days";
        public const string RetainFailedDays   = "cleanup.retain_failed_days";
        public const string MaxStorageGb       = "cleanup.max_storage_gb";
        public const string RunIntervalMinutes = "cleanup.run_interval_minutes";
        public const string DeleteArchived     = "cleanup.delete_archived";
    }

    // ── Transfer ──────────────────────────────────────────────────────────────────
    public static class Transfer
    {
        public const string MaxRetries        = "transfer.max_retries";
        public const string RetryBaseDelaySec = "transfer.retry_base_delay_sec";
        public const string TimeoutSeconds    = "transfer.timeout_seconds";
        public const string MaxConcurrent     = "transfer.max_concurrent";
    }

    // ── Security ──────────────────────────────────────────────────────────────────
    public static class Security
    {
        public const string RequireTls         = "security.require_tls";
        public const string AuditRetentionDays = "security.audit_retention_days";
    }

    // ── Storage ───────────────────────────────────────────────────────────────────
    public static class Storage
    {
        public const string RootPath    = "storage.root_path";
        public const string ArchivePath = "storage.archive_path";
    }
}
