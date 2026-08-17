using Dicom.Edge.Hub.Application.Configuration;
using Dicom.Edge.Hub.Application.Dispatch;
using Dicom.Edge.Hub.Application.Hl7;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Application.Reports;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Dicom.Edge.Hub.Infrastructure.Http;
using Dicom.Edge.Hub.Infrastructure.Services;
using Dicom.Edge.Hub.Infrastructure.Storage;
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

        // Runtime configuration reload (system_settings → IOptionsMonitor without restart).
        services.AddSingleton<IRuntimeConfigReloader, RuntimeConfigReloader>();

        // P0-1: Per-node auth handler that signs outbound Hub→Node requests with HMAC.
        // Cache for the per-node signing key.
        services.AddMemoryCache();
        services.AddScoped<INodeAuthKeyProvider, NodeAuthKeyProvider>();
        services.AddTransient<HubAuthDelegatingHandler>();

        // HTTP client for dispatching to nodes — with standard resilience (retry + circuit breaker)
        services.AddHttpClient(DispatchConstants.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(DispatchConstants.DefaultTimeoutSeconds);
        })
        .AddHttpMessageHandler<HubAuthDelegatingHandler>()
        .AddStandardResilienceHandler();

        services.AddScoped<INodeDispatcher, NodeHttpDispatcher>();

        // Node configuration push service
        services.AddScoped<INodeConfigPushService, NodeConfigPushService>();

        services.AddScoped<IHl7PatientSyncService, Hl7PatientSyncService>();

        // Report storage in the Hub workspace (physical PDFs from ORU OBX ED).
        services.AddOptions<HubWorkspaceOptions>().BindConfiguration(HubWorkspaceOptions.SectionName);
        services.AddSingleton<IReportStorage, HubFileReportStorage>();

        // QR code generation (image-viewer link → PNG).
        services.AddSingleton<IQrCodeGenerator, Dicom.Edge.Hub.Infrastructure.Notifications.QrCodeGenerator>();

        // Multichannel notification senders (siblings; selected by Channel in the dispatcher).
        services.AddOptions<SmtpOptions>().BindConfiguration(SmtpOptions.SectionName);
        services.AddScoped<INotificationChannelSender, Dicom.Edge.Hub.Infrastructure.Notifications.WhatsAppChannelSender>();
        services.AddScoped<INotificationChannelSender, Dicom.Edge.Hub.Infrastructure.Notifications.EmailChannelSender>();

        return services;
    }
}
