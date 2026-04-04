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
}
