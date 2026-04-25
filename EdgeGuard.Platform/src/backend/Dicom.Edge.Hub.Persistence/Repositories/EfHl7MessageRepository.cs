using System.Linq.Expressions;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Common.Sorting;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class EfHl7MessageRepository : IHl7MessageRepository
{
    private readonly HubDbContext _context;

    public EfHl7MessageRepository(HubDbContext context) => _context = context;

    public async Task<Hl7Message> AddAsync(Hl7Message message, CancellationToken cancellationToken = default)
    {
        await _context.Hl7Messages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<Hl7Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<Hl7Message>> GetByStatusAsync(
        Hl7MessageStatus status,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .AsNoTracking()
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.ReceivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Hl7Message>> GetByDispatchStatusAsync(
        Hl7DispatchStatus status,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .AsNoTracking()
            .Where(m => m.DispatchStatus == status)
            .OrderByDescending(m => m.ReceivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Hl7Message>> GetQueuedForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .Where(m => m.DispatchStatus == Hl7DispatchStatus.Queued)
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task UpdateAsync(Hl7Message message, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(message);

        if (entry.State == EntityState.Detached)
            _context.Hl7Messages.Update(message);

        await _context.SaveChangesAsync();
       
    }

    public async Task<IEnumerable<Hl7Message>> GetRecentMessagesAsync(
        int count,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .AsNoTracking()
            .OrderByDescending(m => m.ReceivedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByDispatchStatusAsync(
        Hl7DispatchStatus status,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .CountAsync(m => m.DispatchStatus == status, cancellationToken);

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        var total = 0;
        int deleted;
        do
        {
            deleted = await _context.Hl7Messages
                .Where(m => m.ReceivedAt < cutoff)
                .OrderBy(m => m.ReceivedAt)
                .Take(batchSize)
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
        } while (deleted == batchSize && !cancellationToken.IsCancellationRequested);
        return total;
    }

    public async Task<PagedResult<Hl7Message>> GetFilteredPagedAsync(PaginationRequest pagination, Hl7MessageFilterCriteria filter, CancellationToken ct = default)
    {
        var query = _context.Hl7Messages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.MessageType)) query = query.Where(m => m.MessageType == filter.MessageType);
        if (!string.IsNullOrWhiteSpace(filter.DispatchStatus) && Enum.TryParse<Hl7DispatchStatus>(filter.DispatchStatus, true, out var ds))
            query = query.Where(m => m.DispatchStatus == ds);
        if (!string.IsNullOrWhiteSpace(filter.TargetNodeId)) query = query.Where(m => m.TargetNodeId == filter.TargetNodeId);
        if (filter.DateFrom.HasValue) query = query.Where(m => m.ReceivedAt >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue) query = query.Where(m => m.ReceivedAt <= filter.DateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .ApplySort(filter.SortBy, filter.SortDir, Hl7SortFields, q => q.OrderByDescending(m => m.ReceivedAt))
            .Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct);

        return new PagedResult<Hl7Message> { Items = items, Page = pagination.Page, PageSize = pagination.PageSize, TotalCount = totalCount };
    }

    private static readonly Dictionary<string, Expression<Func<Hl7Message, object?>>> Hl7SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["receivedAt"] = m => m.ReceivedAt,
        ["messageType"] = m => m.MessageType,
        ["dispatchStatus"] = m => m.DispatchStatus,
        ["priority"] = m => m.Priority,
        ["targetNodeId"] = m => m.TargetNodeId,
        ["dispatchAttempts"] = m => m.DispatchAttempts,
    };
}
