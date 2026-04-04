using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class HubAuditLogRepository : IHubAuditLogRepository
{
    private readonly HubDbContext _context;

    public HubAuditLogRepository(HubDbContext context) => _context = context;

    public async Task<HubAuditLog> AddAsync(HubAuditLog entry, CancellationToken ct = default)
    {
        await _context.HubAuditLogs.AddAsync(entry, ct);
        return entry;
    }

    public async Task<IReadOnlyList<HubAuditLog>> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default) =>
        await _context.HubAuditLogs
            .AsNoTracking()
            .Where(a => a.CorrelationId == correlationId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HubAuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default) =>
        await _context.HubAuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HubAuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.HubAuditLogs
            .AsNoTracking()
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HubAuditLog>> GetByEventTypeAsync(AuditEventType eventType, int limit = 100, CancellationToken ct = default) =>
        await _context.HubAuditLogs
            .AsNoTracking()
            .Where(a => a.EventType == eventType)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default)
    {
        var total = 0;
        int deleted;
        do
        {
            deleted = await _context.HubAuditLogs
                .Where(a => a.CreatedAt < cutoff)
                .OrderBy(a => a.CreatedAt)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);
            total += deleted;
        } while (deleted == batchSize && !ct.IsCancellationRequested);
        return total;
    }
}
