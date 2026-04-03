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
}
