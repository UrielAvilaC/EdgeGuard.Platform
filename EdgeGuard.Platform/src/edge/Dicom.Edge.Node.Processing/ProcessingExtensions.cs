using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Processing;

public static class ProcessingExtensions
{
    public static IServiceCollection AddNodeProcessing(this IServiceCollection services)
    {
        services.AddSingleton<IStudyPipeline, StudyPipeline>();
        services.AddHostedService<StudyProcessingHostedService>();

        return services;
    }
}
