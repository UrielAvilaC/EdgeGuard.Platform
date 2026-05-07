using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.Services;
using Dicom.Edge.Hub.Infrastructure.EventHandlers;
using Dicom.Edge.Hub.Infrastructure.Services;
using Dicom.Edge.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Hub.Infrastructure.Extensions;

/// <summary>
/// DI extension methods for Hub domain service implementations.
/// Persistence registration has been moved to <c>Dicom.Edge.Hub.Persistence.Extensions</c>.
/// </summary>
public static class HubDomainServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hub domain service implementations.
    /// </summary>
    public static IServiceCollection AddHubDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IStudyAuditService, StudyAuditService>();
        services.AddScoped<IPacsInheritanceService, PacsInheritanceService>();
        services.AddScoped<IStudyCleanupService, StudyCleanupService>();
        services.AddScoped<INodeHealthEvaluator, NodeHealthEvaluator>();
        services.AddScoped<IHubDataRetentionService, HubDataRetentionService>();

        // Domain event handlers (dispatched by DomainEventDispatchInterceptor)
        services.AddScoped<IDomainEventHandler, AuditDomainEventHandler>();

        // M2M API key validation for Edge Node authentication
        services.AddScoped<IApiKeyValidator, NodeApiKeyValidator>();

        // Node-push services (Hub → Node HTTP)
        services.AddScoped<INodeConfigPushService, NodeConfigPushService>();
        services.AddScoped<INodePacsDestinationPushService, NodePacsDestinationPushService>();

        return services;
    }
}
