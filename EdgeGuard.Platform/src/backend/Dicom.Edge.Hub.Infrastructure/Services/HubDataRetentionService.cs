using System.Diagnostics;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Domain.Services;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Enterprise data retention service. Purges old records from high-volume tables
/// in configurable batches to maintain database performance at millions of records.
/// </summary>
public sealed class HubDataRetentionService(
    IHubAuditLogRepository auditLogRepository,
    IHl7MessageRepository hl7MessageRepository,
    IHealthCheckRepository healthCheckRepository,
    INotificationRepository notificationRepository,
    INodeOutboxRepository nodeOutboxRepository,
    IPacsSendAuditRepository pacsSendAuditRepository,
    IStudyStatusAuditRepository studyStatusAuditRepository,
    IOptionsMonitor<HubBackgroundJobsOptions> options,
    ILogger<HubDataRetentionService> logger) : IHubDataRetentionService
{
    public async Task<DataRetentionResult> ExecuteRetentionAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        // Read the latest policy each cycle so system_settings edits apply without a restart.
        var policy = options.CurrentValue.DataRetention;
        logger.LogInformation("Data retention cycle starting");

        var auditLogs = await PurgeTableAsync(
            "AuditLogs", policy.AuditLogRetentionDays, policy.BatchSize,
            (cutoff, batch, token) => auditLogRepository.DeleteOlderThanAsync(cutoff, batch, token), ct);

        var hl7Messages = await PurgeTableAsync(
            "Hl7Messages", policy.Hl7MessageRetentionDays, policy.BatchSize,
            (cutoff, batch, token) => hl7MessageRepository.DeleteOlderThanAsync(cutoff, batch, token), ct);

        var healthChecks = await PurgeTableAsync(
            "HealthChecks", policy.HealthCheckRetentionDays, policy.BatchSize,
            (cutoff, batch, token) => healthCheckRepository.DeleteOlderThanAsync(cutoff, batch, token), ct);

        var notifications = await PurgeTableAsync(
            "Notifications", policy.NotificationOutboxRetentionDays, policy.BatchSize,
            (cutoff, batch, token) => notificationRepository.DeleteOlderThanAsync(cutoff, batch, token), ct);

        var nodeOutbox = await PurgeTableAsync(
            "NodeOutboxMessages", policy.NodeOutboxRetentionDays, policy.BatchSize,
            (cutoff, batch, token) => nodeOutboxRepository.DeleteOlderThanAsync(cutoff, batch, token), ct);

        var pacsSend = await PurgeTableAsync(
            "PacsSendAudits", policy.PacsSendAuditRetentionDays, policy.BatchSize,
            (cutoff, batch, token) => pacsSendAuditRepository.DeleteOlderThanAsync(cutoff, batch, token), ct);

        var studyStatus = await PurgeTableAsync(
            "StudyStatusAudits", policy.StudyStatusAuditRetentionDays, policy.BatchSize,
            (cutoff, batch, token) => studyStatusAuditRepository.DeleteOlderThanAsync(cutoff, batch, token), ct);

        sw.Stop();

        var result = new DataRetentionResult
        {
            AuditLogsDeleted = auditLogs,
            Hl7MessagesDeleted = hl7Messages,
            HealthChecksDeleted = healthChecks,
            NotificationsDeleted = notifications,
            NodeOutboxDeleted = nodeOutbox,
            PacsSendAuditsDeleted = pacsSend,
            StudyStatusAuditsDeleted = studyStatus,
            Duration = sw.Elapsed
        };

        logger.LogInformation(
            "Data retention cycle completed in {Duration:N1}s — {Total} records purged",
            result.Duration.TotalSeconds, result.TotalDeleted);

        return result;
    }

    private async Task<int> PurgeTableAsync(
        string tableName,
        int retentionDays,
        int batchSize,
        Func<DateTime, int, CancellationToken, Task<int>> deleteFunc,
        CancellationToken ct)
    {
        if (retentionDays <= 0)
        {
            logger.LogDebug("Retention disabled for {Table}, skipping", tableName);
            return 0;
        }

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        logger.LogDebug("Purging {Table} older than {Cutoff:u} (batch={Batch})",
            tableName, cutoff, batchSize);

        try
        {
            var deleted = await deleteFunc(cutoff, batchSize, ct);
            if (deleted > 0)
                logger.LogInformation("Purged {Deleted} records from {Table}", deleted, tableName);
            return deleted;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to purge {Table}", tableName);
            return 0;
        }
    }
}
