namespace Dicom.Edge.Hub.Domain.Aggregates.Outbox;

/// <summary>Read/write access to the <c>node_outbox_messages</c> store.</summary>
public interface INodeOutboxRepository
{
    /// <summary>Pending rows whose <c>NextAttemptAt</c> is due (null or in the past), oldest first.</summary>
    Task<IReadOnlyList<NodeOutboxMessage>> GetDuePendingAsync(int batchSize, CancellationToken ct = default);

    Task<NodeOutboxMessage?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>True if a Pending row already exists for this node + topic (coalescing).</summary>
    Task<bool> HasPendingAsync(string nodeId, string topicId, CancellationToken ct = default);

    Task AddAsync(NodeOutboxMessage message, CancellationToken ct = default);
    Task UpdateAsync(NodeOutboxMessage message, CancellationToken ct = default);

    /// <summary>Bulk-deletes terminal rows (Sent/Failed) older than <paramref name="cutoffUtc"/>.</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken ct = default);
}
