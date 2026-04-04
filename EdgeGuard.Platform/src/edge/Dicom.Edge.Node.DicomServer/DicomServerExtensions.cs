using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.DicomServer;

public static class DicomServerExtensions
{
    public static IServiceCollection AddNodeDicomServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DicomServerOptions>(
            configuration.GetSection(DicomServerOptions.SectionName));

        // MWL C-FIND handler — queries the local worklist and builds DICOM responses
        services.AddSingleton<IWorklistCFindHandler, WorklistCFindHandler>();

        services.AddHostedService<DicomServerHostedService>();

        return services;
    }
}
