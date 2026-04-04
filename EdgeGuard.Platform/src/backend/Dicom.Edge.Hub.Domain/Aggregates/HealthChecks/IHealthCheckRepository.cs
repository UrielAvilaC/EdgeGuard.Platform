namespace Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;

/// <summary>
/// Repository interface for HealthCheckRecord aggregate.
/// </summary>
public interface IHealthCheckRepository
{
    Task<HealthCheckRecord> AddAsync(HealthCheckRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<HealthCheckRecord>> GetByNodeAsync(string nodeId, CancellationToken ct = default);
    Task<HealthCheckRecord?> GetLatestByNodeAsync(string nodeId, CancellationToken ct = default);
    Task<IReadOnlyList<HealthCheckRecord>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Deletes health check records older than the specified cutoff date in batches. Returns the count deleted.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default);
}
