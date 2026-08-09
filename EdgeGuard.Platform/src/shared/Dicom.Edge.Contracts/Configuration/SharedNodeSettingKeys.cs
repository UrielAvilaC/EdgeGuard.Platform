namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// All keys for node settings, grouped by category using nested static classes.
/// Single source of truth shared between Hub and Node.
/// </summary>
public static class SharedNodeSettingKeys
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
        public const string Version      = "node.version";
        public const string ContactEmail = "node.contact_email";
        public const string ContactPhone = "node.contact_phone";
        public const string IpAddress    = "node.ip_address";
        public const string ApiEndpoint  = "node.api_endpoint";
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
        public const string NodeId                = "hub.node_id";
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
        public const string Enabled                   = "dicom.enabled";
        public const string ValidateCallingAe         = "dicom.validate_calling_ae";
        public const string ValidateCalledAe          = "dicom.validate_called_ae";
        public const string AllowedAeTitles           = "dicom.allowed_ae_titles";
        public const string AeTitleAliases            = "dicom.ae_title_aliases";
        public const string MaxAssociations           = "dicom.max_associations";
        public const string Port                      = "dicom.port";
        public const string AeTitle                   = "dicom.ae_title";
        public const string StudyCompletionTimeoutSec = "dicom.study_completion_timeout_sec";
        public const string AssociationTimeoutSec     = "dicom.association_timeout_sec";
        public const string DimseTimeoutSec           = "dicom.dimse_timeout_sec";
        public const string MaxPduLength              = "dicom.max_pdu_length";
        public const string MwlEnabled                = "dicom.mwl_enabled";
        public const string CEchoEnabled              = "dicom.cecho_enabled";
        public const string QrEnabled                 = "dicom.qr_enabled";
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────────
    public static class Diagnostics
    {
        /// <summary>Enables the per-DICOM-association log files.</summary>
        public const string AssocLogEnabled    = "diagnostics.assoc_log_enabled";

        /// <summary>Serilog level written to each association file (Debug by default).</summary>
        public const string AssocLogLevel      = "diagnostics.assoc_log_level";

        /// <summary>Days the association files are kept before cleanup removes them.</summary>
        public const string AssocLogRetainDays = "diagnostics.assoc_log_retain_days";
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

    // ── PACS Sender ──────────────────────────────────────────────────────────────
    public static class PacsSender
    {
        public const string Enabled                   = "sender.enabled";
        public const string LocalAeTitle              = "sender.local_ae_title";
        public const string MaxConcurrentSends        = "sender.max_concurrent_sends";
        public const string TimeoutSeconds            = "sender.timeout_seconds";
        public const string MaxRetries                = "sender.max_retries";
        public const string RetryBaseDelaySeconds     = "sender.retry_base_delay_seconds";
        public const string ProcessingIntervalSeconds = "sender.processing_interval_seconds";
    }

    // ── PACS C-ECHO ──────────────────────────────────────────────────────────────
    public static class PacsCEcho
    {
        public const string Enabled         = "cecho.enabled";
        public const string IntervalSeconds = "cecho.interval_seconds";
        public const string Destinations    = "cecho.destinations";
    }

    // ── Node API ─────────────────────────────────────────────────────────────────
    public static class NodeApi
    {
        public const string Port = "nodeapi.port";
    }

    // ── System (config sync metadata) ────────────────────────────────────────────
    public static class System
    {
        public const string ConfigVersion         = "system.config_version";
        public const string LastConfigAppliedUtc  = "system.last_config_applied_utc";
        public const string LastConfigSource      = "system.last_config_source";
    }
}
