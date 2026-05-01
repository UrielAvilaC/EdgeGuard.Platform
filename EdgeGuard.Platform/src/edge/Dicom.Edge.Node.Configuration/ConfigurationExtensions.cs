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

        // When ApiPort is not explicitly set (0), inherit from NodeApi:Port so that
        // the ApiEndpoint fallback uses the same port Kestrel actually listens on.
        services.PostConfigure<HubConnectionOptions>(opts =>
        {
            if (opts.ApiPort == 0)
                opts.ApiPort = configuration.GetValue("NodeApi:Port", 5120);
        });

        // HttpClient base address and timeout only — API key is added per-request
        // by HubSyncClient (loaded from DB or received during registration)
        services.AddHttpClient<IHubSyncClient, HubSyncClient>((sp, client) =>
        {
            var opts = configuration
                .GetSection(HubConnectionOptions.SectionName)
                .Get<HubConnectionOptions>() ?? new HubConnectionOptions();

            client.BaseAddress = new Uri(opts.HubBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        services.AddHostedService<HubConfigSyncHostedService>();

        return services;
    }
}
