using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Worklist;

public static class WorklistExtensions
{
    public static IServiceCollection AddNodeWorklist(this IServiceCollection services)
    {
        services.AddSingleton<IWorklistManager, InMemoryWorklistManager>();

        return services;
    }
}
