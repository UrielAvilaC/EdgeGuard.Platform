using System.Linq.Expressions;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Common.Sorting;
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

    public async Task<PagedResult<HubAuditLog>> GetPagedAsync(
        PaginationRequest pagination,
        AuditLogFilterCriteria filter,
        CancellationToken ct = default)
    {
        var query = _context.HubAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.EventType) && Enum.TryParse<AuditEventType>(filter.EventType, true, out var eventType))
            query = query.Where(a => a.EventType == eventType);
        if (!string.IsNullOrWhiteSpace(filter.Severity) && Enum.TryParse<AuditSeverity>(filter.Severity, true, out var severity))
            query = query.Where(a => a.Severity == severity);
        if (!string.IsNullOrWhiteSpace(filter.UserId)) query = query.Where(a => a.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.EntityType)) query = query.Where(a => a.EntityType == filter.EntityType);
        if (!string.IsNullOrWhiteSpace(filter.EntityId)) query = query.Where(a => a.EntityId == filter.EntityId);
        if (filter.DateFrom.HasValue) query = query.Where(a => a.CreatedAt >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue) query = query.Where(a => a.CreatedAt <= filter.DateTo.Value);
        if (filter.IsSuccess.HasValue) query = query.Where(a => a.IsSuccess == filter.IsSuccess.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(a =>
                a.Action.Contains(filter.Search) ||
                (a.Details != null && a.Details.Contains(filter.Search)) ||
                (a.UserName != null && a.UserName.Contains(filter.Search)));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .ApplySort(filter.SortBy, filter.SortDir, AuditSortFields, q => q.OrderByDescending(a => a.CreatedAt))
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<HubAuditLog>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    private static readonly Dictionary<string, Expression<Func<HubAuditLog, object?>>> AuditSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["createdAt"] = a => a.CreatedAt,
        ["eventType"] = a => a.EventType,
        ["severity"] = a => a.Severity,
        ["action"] = a => a.Action,
        ["userName"] = a => a.UserName,
        ["entityType"] = a => a.EntityType,
        ["isSuccess"] = a => a.IsSuccess,
    };

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
