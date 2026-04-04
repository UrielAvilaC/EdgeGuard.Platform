using Dicom.Edge.Diagnostics.Bootstrap;
using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.Extensions;
using Dicom.Edge.Hub.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Hub.Diagnostics.Extensions;

/// <summary>
/// Extension methods for registering Hub-specific diagnostics services.
/// Calls <see cref="PlatformDiagnosticsExtensions.AddPlatformDiagnostics"/> for shared infrastructure
/// and adds Hub-specific health checks, log scopes, and metrics.
/// </summary>
public static class HubDiagnosticsExtensions
{
    /// <summary>
    /// Registers all Hub diagnostics services: platform diagnostics + Hub-specific health checks.
    /// </summary>
    public static IServiceCollection AddHubDiagnostics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Shared platform diagnostics (Serilog options, PHI redaction, audit, OTel, storage health)
        services.AddPlatformDiagnostics(configuration);

        // Hub-specific: HL7 listener health check
        services.Configure<Hl7ListenerHealthOptions>(
            configuration.GetSection(Hl7ListenerHealthOptions.SectionName));

        services.AddHealthChecks()
            .AddCheck<Hl7ListenerHealthCheck>(
                HealthCheckConstants.Hl7ListenerCheckName,
                tags: [HealthCheckConstants.ReadyTag, HealthCheckConstants.Hl7Tag]);

        return services;
    }

    /// <summary>
    /// Configures Serilog platform logging for the Hub's WebApplicationBuilder.
    /// </summary>
    public static WebApplicationBuilder UseHubLogging(
        this WebApplicationBuilder builder,
        IConfiguration? configuration = null)
    {
        configuration ??= builder.Configuration;
        builder.Host.UsePlatformLogging(configuration);
        return builder;
    }
}
