using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Infrastructure.Repositories;

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
            .Include(h => h.PacsResults)
            .Where(h => h.NodeId == nodeId)
            .OrderByDescending(h => h.ReceivedAt)
            .ToListAsync(ct);

    public async Task<HealthCheckRecord?> GetLatestByNodeAsync(string nodeId, CancellationToken ct = default) =>
        await _context.HealthCheckRecords
            .Include(h => h.PacsResults)
            .Where(h => h.NodeId == nodeId)
            .OrderByDescending(h => h.ReceivedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<HealthCheckRecord>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.HealthCheckRecords
            .Include(h => h.PacsResults)
            .Where(h => h.ReceivedAt >= from && h.ReceivedAt <= to)
            .OrderByDescending(h => h.ReceivedAt)
            .ToListAsync(ct);
}
