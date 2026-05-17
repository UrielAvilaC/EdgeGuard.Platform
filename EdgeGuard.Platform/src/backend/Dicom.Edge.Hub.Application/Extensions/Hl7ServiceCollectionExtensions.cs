using Dicom.Edge.Hub.Application.Configuration;
using Dicom.Edge.Hub.Application.CsvServices;
using Dicom.Edge.Hub.Application.Dashboard;
using Dicom.Edge.Hub.Application.Edge;
using Dicom.Edge.Hub.Application.Hl7;
using Dicom.Edge.Hub.Application.Hl7.Pipeline;
using Dicom.Edge.Hub.Application.Identity;
using Dicom.Edge.Hub.Application.Nodes;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Application.PacsServers;
using Dicom.Edge.Hub.Application.Queue;
using Dicom.Edge.Hub.Application.Routing;
using Dicom.Edge.Hub.Application.Studies;
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
        services.AddScoped<IHl7PatientSyncService, Hl7PatientSyncService>();
        services.AddScoped<IHl7StudySyncService, Hl7StudySyncService>();

        // Configuration services
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<INodeConfigurationService, NodeConfigurationService>();

        // CRUD application services (write operations)
        services.AddScoped<INodeService, NodeService>();
        services.AddScoped<IPacsServerService, PacsServerService>();
        services.AddScoped<IRoutingRuleService, RoutingRuleService>();
        services.AddScoped<INodeDicomRoutingRuleService, NodeDicomRoutingRuleService>();
        services.AddScoped<IStudyService, StudyService>();

        // Edge node-facing orchestration service
        services.AddScoped<IEdgeNodeService, EdgeNodeService>();
        services.AddScoped<IBootstrapTokenService, BootstrapTokenService>();

        // Identity services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        // Dashboard & CSV services
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<ICsvImportService, CsvImportService>();

        return services;
    }
}
