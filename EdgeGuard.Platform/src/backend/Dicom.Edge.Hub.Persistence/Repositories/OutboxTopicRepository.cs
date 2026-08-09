using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class OutboxTopicRepository(HubDbContext context) : IOutboxTopicRepository
{
    public async Task<OutboxTopic?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        await context.OutboxTopics.FindAsync([key], ct);

    public async Task<IReadOnlyList<OutboxTopic>> GetAllAsync(CancellationToken ct = default) =>
        await context.OutboxTopics
            .OrderBy(t => t.Category)
            .ThenBy(t => t.DisplayName)
            .ToListAsync(ct);

    public async Task AddAsync(OutboxTopic topic, CancellationToken ct = default) =>
        await context.OutboxTopics.AddAsync(topic, ct);
}
