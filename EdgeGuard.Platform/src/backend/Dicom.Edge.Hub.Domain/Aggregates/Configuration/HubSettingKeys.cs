namespace Dicom.Edge.Hub.Domain.Aggregates.Configuration;

/// <summary>
/// Well-known system setting keys for the Hub.
/// </summary>
public static class HubSettingKeys
{
    public static class Hl7
    {
        public const string TcpPort = "hl7.tcp_port";
        public const string TcpEnabled = "hl7.tcp_enabled";
        public const string MaxConcurrentConnections = "hl7.max_concurrent_connections";
        public const string MaxQueuedMessages = "hl7.max_queued_messages";
        public const string ProcessingWorkers = "hl7.processing_workers";
        public const string ConnectionTimeoutMs = "hl7.connection_timeout_ms";
        public const string BufferSize = "hl7.buffer_size";
    }

    public static class Dispatch
    {
        public const string Enabled = "dispatch.enabled";
        public const string BatchSize = "dispatch.batch_size";
        public const string IntervalSeconds = "dispatch.interval_seconds";
        public const string MaxRetries = "dispatch.max_retries";
        public const string RetryDelaySeconds = "dispatch.retry_delay_seconds";
        public const string TimeoutSeconds = "dispatch.timeout_seconds";
    }

    public static class Queue
    {
        public const string MaxPendingMessages = "queue.max_pending_messages";
        public const string PriorityBoostUrgent = "queue.priority_boost_urgent";
        public const string RetentionDays = "queue.retention_days";
    }

    public static class General
    {
        public const string HubName = "general.hub_name";
        public const string HubVersion = "general.hub_version";
        public const string Environment = "general.environment";
    }

    public static class WhatsApp
    {
        public const string Enabled = "whatsapp.enabled";
        public const string EnableAutomaticDelivery = "whatsapp.enable_automatic_delivery";
        public const string Provider = "whatsapp.provider";

        /// <summary>
        /// Encrypted JSON field. Deserialized to <c>MessagingProviderConfig</c> by <c>TwilioMessagingProvider</c>.
        /// Expected shape:
        /// <code>
        /// {
        ///   "AccountSid":          "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
        ///   "AuthToken":           "your_auth_token",
        ///   "PhoneNumber":         "+14155238886",
        ///   "MessagingServiceSid": "MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
        /// }
        /// </code>
        /// </summary>
        public const string ProviderConfig = "whatsapp.provider_config";

        public const string DefaultCountryPrefix = "whatsapp.default_country_prefix";
        public const string RetryMaxAttempts = "whatsapp.retry_max_attempts";
        public const string RetryDelaySeconds = "whatsapp.retry_delay_seconds";
    }

    public static class Notifications
    {
        /// <summary>Master switch for automatic results delivery on study finalization.</summary>
        public const string AutoMode = "notifications.auto_mode";
    }

    public static class BackgroundJobs
    {
        public const string EnableNodeHealth = "jobs.enable_node_health";
        public const string NodeHealthIntervalSec = "jobs.node_health_interval_sec";
        public const string EnableStudyCleanup = "jobs.enable_study_cleanup";
        public const string StudyCleanupIntervalSec = "jobs.study_cleanup_interval_sec";
        public const string EnableDataRetention = "jobs.enable_data_retention";
        public const string DataRetentionIntervalSec = "jobs.data_retention_interval_sec";
    }

    public static class DataRetention
    {
        public const string AuditLogDays = "retention.audit_log_days";
        public const string Hl7MessageDays = "retention.hl7_message_days";
        public const string HealthCheckDays = "retention.health_check_days";
        public const string NotificationDays = "retention.notification_days";
        public const string NodeOutboxDays = "retention.node_outbox_days";
        public const string PacsSendAuditDays = "retention.pacs_send_audit_days";
        public const string StudyStatusAuditDays = "retention.study_status_audit_days";
        public const string BatchSize = "retention.batch_size";
    }
}
