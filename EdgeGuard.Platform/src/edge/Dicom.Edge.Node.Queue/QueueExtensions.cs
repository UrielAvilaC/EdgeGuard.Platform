using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Queue;

public static class QueueExtensions
{
    public static IServiceCollection AddNodeQueue(this IServiceCollection services)
    {
        // INodeWorkQueue implementation is provided by Persistence layer
        // (SqliteEdgeQueue adapts IEdgeQueue<EdgeQueueItem>).
        // This extension is a placeholder for future in-memory or Redis queue overrides.
        return services;
    }
}
