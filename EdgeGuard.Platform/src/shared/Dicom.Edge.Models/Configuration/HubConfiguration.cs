namespace Dicom.Edge.Models.Configuration
{
    /// <summary>
    /// Global configuration settings for the EdgeGuard Hub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Central configuration that applies across all Edge Nodes and Hub operations.
    /// Settings can be overridden per node using EdgeNode.ConfigurationOverrides.
    /// </para>
    /// </remarks>
    public class HubConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the global default retention period in days.
        /// </summary>
        /// <remarks>
        /// How long to keep studies before archiving/deletion. Default: 90 days.
        /// </remarks>
        public int RetentionDays { get; set; } = 90;

        /// <summary>
        /// Gets or sets the maximum concurrent transfers allowed per Edge Node.
        /// </summary>
        public int MaxConcurrentTransfersPerNode { get; set; } = 5;

        /// <summary>
        /// Gets or sets the default transfer timeout in minutes.
        /// </summary>
        public int TransferTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum retry attempts for failed transfers.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 10;

        /// <summary>
        /// Gets or sets the base retry delay in minutes (exponential backoff).
        /// </summary>
        /// <remarks>
        /// Actual delay = BaseRetryDelayMinutes * 2^(retryAttempt)
        /// </remarks>
        public int BaseRetryDelayMinutes { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether to automatically archive studies after successful transfer.
        /// </summary>
        public bool AutoArchiveAfterTransfer { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum storage threshold percentage before alerting.
        /// </summary>
        /// <remarks>
        /// Alert when storage usage exceeds this percentage. Default: 80%
        /// </remarks>
        public int StorageWarningThresholdPercent { get; set; } = 80;

        /// <summary>
        /// Gets or sets whether to require TLS/SSL for all communications.
        /// </summary>
        public bool RequireTls { get; set; } = true;

        /// <summary>
        /// Gets or sets the health check interval in seconds.
        /// </summary>
        /// <remarks>
        /// How often Edge Nodes should report health status. Default: 60 seconds.
        /// </remarks>
        public int HealthCheckIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the offline threshold in minutes.
        /// </summary>
        /// <remarks>
        /// Consider a node offline if no health report received within this time. Default: 5 minutes.
        /// </remarks>
        public int OfflineThresholdMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to enable audit logging.
        /// </summary>
        public bool EnableAuditLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets audit log retention in days.
        /// </summary>
        /// <remarks>
        /// HIPAA requires 6 years, GDPR varies. Default: 2555 days (7 years).
        /// </remarks>
        public int AuditLogRetentionDays { get; set; } = 2555;

        /// <summary>
        /// Gets or sets whether to automatically register new Edge Nodes.
        /// </summary>
        /// <remarks>
        /// False = Nodes must be manually approved. Recommended for production.
        /// </remarks>
        public bool AutoRegisterNodes { get; set; } = false;

        /// <summary>
        /// Gets or sets the compression level for study transfers.
        /// </summary>
        /// <remarks>
        /// 0 = None, 1 = Fastest, 5 = Balanced, 9 = Best compression
        /// </remarks>
        public int CompressionLevel { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to enable data encryption at rest.
        /// </summary>
        public bool EnableEncryptionAtRest { get; set; } = true;

        /// <summary>
        /// Gets or sets the default PACS destination AE Title.
        /// </summary>
        public string? DefaultPacsAeTitle { get; set; }

        /// <summary>
        /// Gets or sets notification email addresses (comma-separated).
        /// </summary>
        /// <remarks>
        /// Used for alerting on critical errors, node offline, etc.
        /// </remarks>
        public string? NotificationEmails { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this configuration was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the user who last updated this configuration.
        /// </summary>
        public string? UpdatedBy { get; set; }
    }
}
