using Dicom.Edge.Hub.Application.Configuration;
using Dicom.Edge.Hub.Application.Hl7;
using Dicom.Edge.Hub.Application.Hl7.Pipeline;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Application.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Hub.Application.Extensions;

/// <summary>
/// DI extension for all Hub application-layer services.
/// </summary>
public static class HubApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddHubApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HL7 options
        services.Configure<Hl7ListenerOptions>(
            configuration.GetSection(Hl7ListenerOptions.SectionName));

        // Queue options
        services.Configure<MessageQueueOptions>(
            configuration.GetSection(MessageQueueOptions.SectionName));

        // HL7 pipeline services
        services.AddScoped<IHl7MessageProcessor, Hl7MessageProcessor>();
        services.AddScoped<IHl7MonitoringService, Hl7MonitoringService>();
        services.AddScoped<IHl7ValidationService, Hl7ValidationService>();
        services.AddScoped<IHl7RoutingEngine, Hl7RoutingEngine>();

        // Configuration service
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<INodeConfigurationService, NodeConfigurationService>();

        return services;
    }
}
