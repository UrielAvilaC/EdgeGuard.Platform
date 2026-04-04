using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Domain.Aggregates.Cleanup;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Hub.Persistence.Interceptors;
using Dicom.Edge.Hub.Persistence.Repositories;
using Dicom.Edge.Hub.Persistence.Seed;
using Dicom.Edge.Hub.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Hub.Persistence.Extensions;

/// <summary>
/// DI extension methods for Hub persistence layer.
/// Registers DbContext, interceptors, Unit of Work, and all repository implementations.
/// </summary>
public static class HubPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Hub DbContext with PostgreSQL, all interceptors,
    /// Unit of Work, and repository implementations.
    /// </summary>
    public static IServiceCollection AddHubPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Interceptors
        services.AddSingleton<TimestampInterceptor>();
        services.AddSingleton<DomainEventDispatchInterceptor>();

        // DbContext with PostgreSQL + interceptors
        services.AddDbContext<HubDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("HubDatabase");
            options.UseNpgsql(connectionString);
            options.AddInterceptors(
                sp.GetRequiredService<TimestampInterceptor>(),
                sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        // Unit of Work
        services.AddScoped<IUnitOfWork, HubUnitOfWork>();

        // Repositories
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<INodeRepository, NodeRepository>();
        services.AddScoped<IPacsServerRepository, PacsServerRepository>();
        services.AddScoped<IStudyRepository, StudyRepository>();
        services.AddScoped<IStudyStatusAuditRepository, StudyStatusAuditRepository>();
        services.AddScoped<IHealthCheckRepository, HealthCheckRepository>();
        services.AddScoped<IStudyCleanupPolicyRepository, StudyCleanupPolicyRepository>();

        // HL7 / Configuration / Routing repositories
        services.AddScoped<IHl7MessageRepository, EfHl7MessageRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IHl7RoutingRuleRepository, Hl7RoutingRuleRepository>();

        // Audit / Notification / PACS audit repositories
        services.AddScoped<IHubAuditLogRepository, HubAuditLogRepository>();
        services.AddScoped<IWhatsAppNotificationRepository, WhatsAppNotificationRepository>();
        services.AddScoped<IPacsSendAuditRepository, PacsSendAuditRepository>();

        // Node configuration profiles
        services.AddScoped<INodeConfigurationProfileRepository, NodeConfigurationProfileRepository>();

        return services;
    }

    /// <summary>
    /// Seeds missing system settings on startup (safe for upgrades).
    /// Call after the database has been migrated.
    /// </summary>
    public static async Task SeedHubSettingsAsync(this IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<HubDbContext>();
        await HubSettingsSeed.SeedMissingAsync(ctx, ct);
    }
}
