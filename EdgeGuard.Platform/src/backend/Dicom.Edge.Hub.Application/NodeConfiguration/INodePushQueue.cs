namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// In-memory queue that decouples configuration pushes from the HTTP request that
/// triggered them. Producers (application services) <see cref="Enqueue"/> and return
/// immediately; a background dispatcher consumes via <see cref="DequeueAllAsync"/>
/// and performs the actual HTTP push to the node.
/// Registered as a singleton.
/// </summary>
public interface INodePushQueue
{
    /// <summary>Enqueues a push request. Never blocks; returns immediately.</summary>
    void Enqueue(NodePushRequest request);

    /// <summary>
    /// Asynchronously yields queued requests until <paramref name="ct"/> is cancelled.
    /// Intended for a single background consumer.
    /// </summary>
    IAsyncEnumerable<NodePushRequest> DequeueAllAsync(CancellationToken ct);
}
