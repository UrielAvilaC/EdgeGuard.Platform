using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Cleanup;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Services;
using Dicom.Edge.Hub.Infrastructure.Persistence;
using Dicom.Edge.Hub.Infrastructure.Persistence.Interceptors;
using Dicom.Edge.Hub.Infrastructure.Repositories;
using Dicom.Edge.Hub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Hub.Infrastructure.Extensions;

/// <summary>
/// DI extension methods for Hub persistence and domain services.
/// </summary>
public static class HubPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Hub DbContext with PostgreSQL and all interceptors.
    /// </summary>
    public static IServiceCollection AddHubPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<TimestampInterceptor>();
        services.AddSingleton<DomainEventDispatchInterceptor>();

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

        return services;
    }

    /// <summary>
    /// Registers Hub domain service implementations.
    /// </summary>
    public static IServiceCollection AddHubDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IStudyAuditService, StudyAuditService>();
        services.AddScoped<IPacsInheritanceService, PacsInheritanceService>();
        services.AddScoped<IStudyCleanupService, StudyCleanupService>();
        services.AddScoped<INodeHealthEvaluator, NodeHealthEvaluator>();

        return services;
    }
}
