using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class NodeTelemetryRepository(HubDbContext context) : INodeTelemetryRepository
{
    public async Task AddAsync(NodeTelemetryRecord record, CancellationToken ct = default)
    {
        await context.NodeTelemetryRecords.AddAsync(record, ct);
    }

    public async Task<IReadOnlyList<NodeTelemetryRecord>> GetByNodeAsync(
        string nodeId, int limit = 50, CancellationToken ct = default) =>
        await context.NodeTelemetryRecords
            .AsNoTracking()
            .Where(r => r.NodeId == nodeId)
            .OrderByDescending(r => r.ReportedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<NodeTelemetryRecord?> GetLatestByNodeAsync(string nodeId, CancellationToken ct = default) =>
        await context.NodeTelemetryRecords
            .AsNoTracking()
            .Where(r => r.NodeId == nodeId)
            .OrderByDescending(r => r.ReportedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default) =>
        await context.NodeTelemetryRecords
            .Where(r => r.ReportedAt < cutoff)
            .ExecuteDeleteAsync(ct);
}
