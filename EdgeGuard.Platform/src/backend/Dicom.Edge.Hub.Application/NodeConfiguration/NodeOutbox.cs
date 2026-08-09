using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Persists a pending <see cref="NodeOutboxMessage"/> for a node push, coalescing duplicate
/// pending rows for the same node + topic. Durable replacement for the in-memory queue.
/// </summary>
public sealed class NodeOutbox(
    INodeOutboxRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<NodeOutbox> logger) : INodeOutbox
{
    public async Task EnqueueAsync(string nodeId, NodePushKind kind, CancellationToken ct = default)
    {
        var topicId = kind.ToTopicId();

        // Coalesce: a pending push of this kind for this node already covers the change.
        if (await repository.HasPendingAsync(nodeId, topicId, ct))
        {
            logger.LogDebug("Node outbox push {Topic} for {NodeId} coalesced (pending exists)", topicId, nodeId);
            return;
        }

        await repository.AddAsync(NodeOutboxMessage.Create(nodeId, topicId), ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogDebug("Node outbox push {Topic} enqueued for {NodeId}", topicId, nodeId);
    }
}
