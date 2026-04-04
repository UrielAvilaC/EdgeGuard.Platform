namespace Dicom.Edge.Hub.Domain.Services;

/// <summary>
/// Enterprise data retention service. Purges old records from high-volume tables
/// in configurable batches to maintain database performance at millions of records.
/// </summary>
public interface IHubDataRetentionService
{
    /// <summary>
    /// Executes a full retention cycle across all configured tables.
    /// Returns a summary of records purged per table.
    /// </summary>
    Task<DataRetentionResult> ExecuteRetentionAsync(CancellationToken ct = default);
}

/// <summary>
/// Summary of a data retention execution cycle.
/// </summary>
public sealed class DataRetentionResult
{
    public int AuditLogsDeleted { get; init; }
    public int Hl7MessagesDeleted { get; init; }
    public int HealthChecksDeleted { get; init; }
    public int WhatsAppNotificationsDeleted { get; init; }
    public int PacsSendAuditsDeleted { get; init; }
    public int StudyStatusAuditsDeleted { get; init; }
    public TimeSpan Duration { get; init; }

    public int TotalDeleted =>
        AuditLogsDeleted + Hl7MessagesDeleted + HealthChecksDeleted +
        WhatsAppNotificationsDeleted + PacsSendAuditsDeleted + StudyStatusAuditsDeleted;
}
