using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class NodeOutboxRepository(HubDbContext context) : INodeOutboxRepository
{
    public async Task<IReadOnlyList<NodeOutboxMessage>> GetDuePendingAsync(int batchSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await context.NodeOutboxMessages
            .Where(m => m.Status == NodeOutboxStatus.Pending
                     && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<NodeOutboxMessage?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await context.NodeOutboxMessages.FindAsync([id], ct);

    public async Task<bool> HasPendingAsync(string nodeId, string topicId, CancellationToken ct = default) =>
        await context.NodeOutboxMessages.AnyAsync(
            m => m.NodeId == nodeId && m.TopicId == topicId && m.Status == NodeOutboxStatus.Pending, ct);

    public async Task AddAsync(NodeOutboxMessage message, CancellationToken ct = default) =>
        await context.NodeOutboxMessages.AddAsync(message, ct);

    public Task UpdateAsync(NodeOutboxMessage message, CancellationToken ct = default)
    {
        context.NodeOutboxMessages.Update(message);
        return Task.CompletedTask;
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken ct = default)
    {
        var total = 0;
        int deleted;
        do
        {
            // Only purge terminal rows (Sent/Failed); never delete a pending push.
            deleted = await context.NodeOutboxMessages
                .Where(m => m.Status != NodeOutboxStatus.Pending && m.CreatedAt < cutoffUtc)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);
            total += deleted;
        } while (deleted == batchSize && !ct.IsCancellationRequested);
        return total;
    }
}
