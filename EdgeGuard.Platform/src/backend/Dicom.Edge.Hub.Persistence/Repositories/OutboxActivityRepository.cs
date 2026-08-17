using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class OutboxActivityRepository(HubDbContext context) : IOutboxActivityRepository
{
    public async Task<(IReadOnlyList<OutboxActivity> Items, int Total)> QueryAsync(
        string? category,
        string? topicId,
        string? status,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = context.OutboxActivity.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);
        if (!string.IsNullOrWhiteSpace(topicId))
            query = query.Where(a => a.TopicId == topicId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }
}
