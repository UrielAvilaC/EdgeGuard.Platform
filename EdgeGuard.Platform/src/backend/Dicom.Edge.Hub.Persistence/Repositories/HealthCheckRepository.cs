using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class HealthCheckRepository : IHealthCheckRepository
{
    private readonly HubDbContext _context;

    public HealthCheckRepository(HubDbContext context) => _context = context;

    public async Task<HealthCheckRecord> AddAsync(HealthCheckRecord record, CancellationToken ct = default)
    {
        await _context.HealthCheckRecords.AddAsync(record, ct);
        return record;
    }

    public async Task<IReadOnlyList<HealthCheckRecord>> GetByNodeAsync(string nodeId, CancellationToken ct = default) =>
        await _context.HealthCheckRecords
            .AsNoTracking()
            .Include(h => h.PacsResults)
            .Where(h => h.NodeId == nodeId)
            .OrderByDescending(h => h.ReceivedAt)
            .ToListAsync(ct);

    public async Task<HealthCheckRecord?> GetLatestByNodeAsync(string nodeId, CancellationToken ct = default) =>
        await _context.HealthCheckRecords
            .AsNoTracking()
            .Include(h => h.PacsResults)
            .Where(h => h.NodeId == nodeId)
            .OrderByDescending(h => h.ReceivedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<HealthCheckRecord>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.HealthCheckRecords
            .AsNoTracking()
            .Include(h => h.PacsResults)
            .Where(h => h.ReceivedAt >= from && h.ReceivedAt <= to)
            .OrderByDescending(h => h.ReceivedAt)
            .ToListAsync(ct);

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default)
    {
        var total = 0;
        int deleted;
        do
        {
            deleted = await _context.HealthCheckRecords
                .Where(h => h.ReceivedAt < cutoff)
                .OrderBy(h => h.ReceivedAt)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);
            total += deleted;
        } while (deleted == batchSize && !ct.IsCancellationRequested);
        return total;
    }
}
