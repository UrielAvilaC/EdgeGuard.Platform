namespace Dicom.Edge.Hub.Domain.Aggregates.Audit;

/// <summary>
/// Repository interface for Hub audit log entries.
/// </summary>
public interface IHubAuditLogRepository
{
    Task<HubAuditLog> AddAsync(HubAuditLog entry, CancellationToken ct = default);
    Task<IReadOnlyList<HubAuditLog>> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
    Task<IReadOnlyList<HubAuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default);
    Task<IReadOnlyList<HubAuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<HubAuditLog>> GetByEventTypeAsync(AuditEventType eventType, int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Deletes audit logs older than the specified cutoff date in batches. Returns the count deleted.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default);
}
