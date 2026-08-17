namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Writes durable node-sync push requests to the outbox. Replaces the in-memory
/// <c>INodePushQueue</c>: rows survive restarts and are drained by the node outbox
/// dispatcher with retry/backoff. Duplicate pending pushes for the same node + kind
/// are coalesced.
/// </summary>
public interface INodeOutbox
{
    Task EnqueueAsync(string nodeId, NodePushKind kind, CancellationToken ct = default);
}
