using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Worklist;

public static class WorklistExtensions
{
    public static IServiceCollection AddNodeWorklist(this IServiceCollection services)
    {
        // IWorklistManager is registered by the Persistence layer (SqliteWorklistManager).
        return services;
    }
}
