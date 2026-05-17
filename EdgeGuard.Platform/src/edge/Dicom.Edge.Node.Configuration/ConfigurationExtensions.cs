using Dicom.Edge.Abstractions.Monitoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
namespace Dicom.Edge.Node.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddNodeConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HubConnectionOptions>(
            configuration.GetSection(HubConnectionOptions.SectionName));

        services.PostConfigure<HubConnectionOptions>(opts =>
        {
            if (opts.ApiPort == 0)
                opts.ApiPort = configuration.GetValue("NodeApi:Port", 5120);
        });

        services.AddHttpClient<IHubSyncClient, HubSyncClient>((sp, client) =>
        {
            var opts = configuration
                .GetSection(HubConnectionOptions.SectionName)
                .Get<HubConnectionOptions>() ?? new HubConnectionOptions();

            client.BaseAddress = new Uri(opts.HubBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<IStudyHubNotifier, StudyHubNotifier>();
        services.AddSingleton<IPacsEchoHubReporter, PacsEchoHubReporter>();
        services.AddHostedService<HubConfigSyncHostedService>();

        return services;
    }
}
