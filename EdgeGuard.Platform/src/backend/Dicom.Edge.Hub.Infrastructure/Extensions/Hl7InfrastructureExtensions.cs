using Dicom.Edge.Hub.Application.Dispatch;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Dicom.Edge.Hub.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Dicom.Edge.Hub.Infrastructure.Extensions;

/// <summary>
/// Extension methods para configurar la infraestructura de HL7.
/// </summary>
public static class Hl7InfrastructureExtensions
{
    /// <summary>
    /// Registers HL7 infrastructure services: TCP listener, HTTP dispatch client,
    /// and the node dispatcher. Requires persistence to be registered separately
    /// via <see cref="HubPersistenceServiceCollectionExtensions.AddHubPersistence"/>.
    /// </summary>
    public static IServiceCollection AddHl7Infrastructure(this IServiceCollection services)
    {
        // TCP listener
        services.AddSingleton<IHl7Listener, Hl7TcpListener>();
        services.AddHostedService<Hl7ListenerHostedService>();

        // HTTP client for dispatching to nodes — with standard resilience (retry + circuit breaker)
        services.AddHttpClient(DispatchConstants.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(DispatchConstants.DefaultTimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        services.AddScoped<INodeDispatcher, NodeHttpDispatcher>();

        // Node configuration push service
        services.AddScoped<INodeConfigPushService, NodeConfigPushService>();

        return services;
    }
}
