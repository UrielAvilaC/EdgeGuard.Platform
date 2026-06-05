using System.Threading.Channels;

namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Unbounded, single-reader channel implementation of <see cref="INodePushQueue"/>.
/// Safe for concurrent producers; intended for one background consumer.
/// </summary>
public sealed class NodePushQueue : INodePushQueue
{
    private readonly Channel<NodePushRequest> _channel =
        Channel.CreateUnbounded<NodePushRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public void Enqueue(NodePushRequest request)
    {
        // Unbounded channel: TryWrite only fails if the writer has been completed,
        // which never happens for this singleton — so the push is never dropped.
        _channel.Writer.TryWrite(request);
    }

    public IAsyncEnumerable<NodePushRequest> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
