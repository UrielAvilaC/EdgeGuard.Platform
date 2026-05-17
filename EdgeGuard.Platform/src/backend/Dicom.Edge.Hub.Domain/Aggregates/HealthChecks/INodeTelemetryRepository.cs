namespace Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;

/// <summary>
/// Repository for <see cref="NodeTelemetryRecord"/> aggregate.
/// </summary>
public interface INodeTelemetryRepository
{
    Task AddAsync(NodeTelemetryRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<NodeTelemetryRecord>> GetByNodeAsync(string nodeId, int limit = 50, CancellationToken ct = default);
    Task<NodeTelemetryRecord?> GetLatestByNodeAsync(string nodeId, CancellationToken ct = default);
    Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
