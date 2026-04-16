using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dicom.Edge.Node.Queue;

public static class QueueExtensions
{
    public static IServiceCollection AddNodeQueue(this IServiceCollection services)
    {
        // INodeWorkQueue is registered by the Persistence layer (SqliteNodeWorkQueue).
        // TryAdd ensures no duplicate registration if Persistence already registered it.
        // If Persistence is not used, a consumer must register their own INodeWorkQueue.
        return services;
    }
}
