using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Configuration;

public static class ConfigurationExtensions
{
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
                client.DefaultRequestHeaders.Add("X-Api-Key", opts.ApiKey);
        });

        services.AddHostedService<HubConfigSyncHostedService>();

        return services;
    }
}
