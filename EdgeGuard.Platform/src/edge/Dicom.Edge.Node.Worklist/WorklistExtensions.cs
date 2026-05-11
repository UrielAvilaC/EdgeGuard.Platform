using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dicom.Edge.Node.Worklist;

public static class WorklistExtensions
{
    public static IServiceCollection AddNodeWorklist(this IServiceCollection services)
    {
        // IWorklistManager is registered by the Persistence layer (SqliteWorklistManager).
        // TryAdd ensures the in-memory fallback is only used when Persistence did NOT register one.
        services.TryAddSingleton<IWorklistManager, InMemoryWorklistManager>();

        return services;
    }
}
