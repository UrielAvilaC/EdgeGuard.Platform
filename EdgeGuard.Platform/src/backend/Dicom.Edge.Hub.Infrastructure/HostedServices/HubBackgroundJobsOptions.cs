namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

public sealed class HubBackgroundJobsOptions
{
    public const string SectionName = "HubBackgroundJobs";

    public bool EnableNodeHealthEvaluator { get; set; } = true;
    public int NodeHealthEvaluationIntervalSeconds { get; set; } = 60;

    public bool EnableStudyCleanupEvaluator { get; set; } = true;
    public int StudyCleanupEvaluationIntervalSeconds { get; set; } = 300;

    public bool EnableDataRetention { get; set; } = true;
    public int DataRetentionIntervalSeconds { get; set; } = 3600;
    public DataRetentionPolicyOptions DataRetention { get; set; } = new();
}

/// <summary>
/// Configurable retention policies for high-volume tables.
/// Each value represents how many days to retain records before purging.
/// Set to 0 to disable purging for that table.
/// </summary>
public sealed class DataRetentionPolicyOptions
{
    public int AuditLogRetentionDays { get; set; } = 90;
    public int Hl7MessageRetentionDays { get; set; } = 30;
    public int HealthCheckRetentionDays { get; set; } = 30;
    /// <summary>Days to retain rows in the <c>notifications</c> outbox (Email + WhatsApp). 0 disables.</summary>
    public int NotificationOutboxRetentionDays { get; set; } = 60;

    /// <summary>
    /// Days to retain terminal (Sent / DeadLettered) node-outbox rows before purging.
    /// Node-sync history is operational telemetry, so it defaults shorter than messaging.
    /// Set to 0 to disable. Consumed once the node outbox lands (see outbox-unification-plan).
    /// </summary>
    public int NodeOutboxRetentionDays { get; set; } = 14;
    public int PacsSendAuditRetentionDays { get; set; } = 90;
    public int StudyStatusAuditRetentionDays { get; set; } = 90;
    public int BatchSize { get; set; } = 1000;
}
