using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.HealthChecks;
using Dicom.Edge.Diagnostics.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dicom.Edge.Diagnostics.Extensions;

/// <summary>
/// Extension methods for registering Edge Node diagnostics services.
/// Delegates to shared <see cref="PlatformDiagnosticsExtensions"/> and adds
/// Node-specific services (DICOM metrics, PACS health check).
/// </summary>
public static class EdgeDiagnosticsExtensions
{
    /// <summary>
    /// Registers all Edge Node diagnostics services: platform diagnostics + Node-specific extras.
    /// </summary>
    public static IServiceCollection AddEdgeDiagnostics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Shared platform diagnostics (options, PHI, audit, OTel, storage health)
        services.AddPlatformDiagnostics(configuration);

        // Node-specific: DICOM metrics collector
        services.AddSingleton<IMetricsCollector, OpenTelemetryMetricsCollector>();

        // Node-specific: PACS connectivity health check
        services.Configure<PacsConnectivityOptions>(
            configuration.GetSection(PacsConnectivityOptions.SectionName));

        services.AddHealthChecks()
            .AddCheck<PacsConnectivityHealthCheck>(
                HealthCheckConstants.PacsCheckName,
                tags: [HealthCheckConstants.ReadyTag, HealthCheckConstants.PacsTag]);

        return services;
    }

    /// <summary>
    /// Configures Serilog as the logging provider for <see cref="HostApplicationBuilder"/>
    /// (used in Worker Services). Delegates to shared platform logging.
    /// </summary>
    public static HostApplicationBuilder UseEdgeLogging(
        this HostApplicationBuilder builder,
        IConfiguration configuration)
    {
        return builder.UsePlatformLogging(configuration);
    }

    /// <summary>
    /// Configures Serilog as the logging provider on <see cref="IHostBuilder"/>.
    /// Delegates to shared platform logging.
    /// </summary>
    public static IHostBuilder UseEdgeLogging(
        this IHostBuilder hostBuilder,
        IConfiguration configuration)
    {
        return hostBuilder.UsePlatformLogging(configuration);
    }
}
