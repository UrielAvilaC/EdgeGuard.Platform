using Dicom.Edge.Hub.Application.Queue;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Hub.Infrastructure.Extensions;

public static class HubHostedServicesExtensions
{
    public static IServiceCollection AddHubHostedServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HubBackgroundJobsOptions>(
            configuration.GetSection(HubBackgroundJobsOptions.SectionName));

        services.Configure<MessageQueueOptions>(
            configuration.GetSection(MessageQueueOptions.SectionName));

        services.AddHostedService<NodeHealthEvaluationHostedService>();
        services.AddHostedService<StudyCleanupEvaluationHostedService>();
        services.AddHostedService<MessageDispatchHostedService>();
        services.AddHostedService<DataRetentionHostedService>();

        return services;
    }
}
