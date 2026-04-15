using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Dicom.Edge.Node.Configuration;

public static class ConfigurationExtensions
{
    /// <summary>API key header name for Hub authentication.</summary>
    private const string ApiKeyHeaderName = "X-Api-Key";

    public static IServiceCollection AddNodeConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HubConnectionOptions>(
            configuration.GetSection(HubConnectionOptions.SectionName));

        services.AddHttpClient<IHubSyncClient, HubSyncClient>((sp, client) =>
        {
            var opts = configuration
                .GetSection(HubConnectionOptions.SectionName)
                .Get<HubConnectionOptions>() ?? new HubConnectionOptions();

            client.BaseAddress = new Uri(opts.HubBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            if (!string.IsNullOrEmpty(opts.ApiKey))
                client.DefaultRequestHeaders.Add(ApiKeyHeaderName, opts.ApiKey);
        })
        .AddStandardResilienceHandler();

        services.AddHostedService<HubConfigSyncHostedService>();

        return services;
    }
}
