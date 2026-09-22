using Dicom.Edge.Abstractions.Configuration;
using Dicom.Edge.Abstractions.Monitoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
namespace Dicom.Edge.Node.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddNodeConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<HubConnectionOptions>()
            .Bind(configuration.GetSection(HubConnectionOptions.SectionName))
            .PostConfigure(opts =>
            {
                if (opts.ApiPort == 0)
                    opts.ApiPort = configuration.GetValue("NodeApi:Port", 5120);

                // AE unification: the AE reported to the Hub at registration is DERIVED
                // from the single source of truth (DicomServer:AeTitle), never set
                // independently. Keeps the registered Node.AeTitle == SCP/SCU AE.
                opts.AeTitle = configuration[NodeAeTitle.ConfigPath] ?? NodeAeTitle.Default;
            })
            // Fail fast instead of starting with an unusable Hub address — see the validator.
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<HubConnectionOptions>, HubConnectionOptionsValidator>();

        services.AddHttpClient<IHubSyncClient, HubSyncClient>((sp, client) =>
        {
            // Desde IOptions, no reenlazando la sección a mano. Reenlazarla se salta
            // el PostConfigure de arriba y, sobre todo, al validador: con HubBaseUrl
            // vacío este delegado lanzaba un UriFormatException ("The URI is empty")
            // con traza de DI, en lugar del mensaje del validador que nombra la clave
            // y dice dónde ponerla. La dirección vacía es justo el caso de un nodo
            // desplegado sin que el instalador escribiera su configuración, así que
            // es el mensaje que más veces se va a leer.
            var opts = sp.GetRequiredService<IOptions<HubConnectionOptions>>().Value;

            client.BaseAddress = new Uri(opts.HubBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<IStudyHubNotifier, StudyHubNotifier>();
        services.AddSingleton<IPacsEchoHubReporter, PacsEchoHubReporter>();
        services.AddHostedService<HubConfigSyncHostedService>();
        services.AddHostedService<HubHealthReportHostedService>();

        // Equipment presence: report last-seen deltas to the Hub on a periodic interval.
        services.Configure<EquipmentPresenceOptions>(
            configuration.GetSection(EquipmentPresenceOptions.SectionName));
        services.AddHostedService<EquipmentStatusReportHostedService>();

        return services;
    }
}
