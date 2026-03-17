using Dicom.Edge.Abstractions.Audit;
using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Node.Diagnostics.Audit;
using Dicom.Edge.Node.Diagnostics.Configuration;
using Dicom.Edge.Node.Diagnostics.Enrichers;
using Dicom.Edge.Node.Diagnostics.HealthChecks;
using Dicom.Edge.Node.Diagnostics.Observability;
using Dicom.Edge.Node.Diagnostics.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Dicom.Edge.Node.Diagnostics.Extensions;

/// <summary>
/// Extension methods for registering Edge Node diagnostics services
/// including logging, health checks, metrics and PHI redaction.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Registers all Edge Node diagnostics services into the DI container:
    /// options, PHI redaction, health checks, metrics collector and OpenTelemetry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEdgeDiagnostics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration sections
        var diagnosticsSection = configuration.GetSection(DiagnosticsOptions.SectionName);
        services.Configure<DiagnosticsOptions>(diagnosticsSection);
        services.Configure<PhiRedactionOptions>(diagnosticsSection.GetSection("Redaction"));
        services.Configure<FileLoggingOptions>(diagnosticsSection.GetSection("File"));
        services.Configure<HealthCheckOptions>(diagnosticsSection.GetSection("HealthChecks"));
        services.Configure<PacsConnectivityOptions>(
            configuration.GetSection(PacsConnectivityOptions.SectionName));

        // Configuration validation — fail-fast on invalid settings
        services.AddSingleton<IValidateOptions<DiagnosticsOptions>, DiagnosticsOptionsValidator>();

        // PHI redaction
        services.AddSingleton<IPhiProtector, RegexPhiProtector>();

        // Metrics
        services.AddSingleton<IMetricsCollector, OpenTelemetryMetricsCollector>();

        // Audit logging (HIPAA/GDPR compliance)
        services.AddSingleton<IAuditLogger, StructuredAuditLogger>();

        // Health checks
        services.AddHealthChecks()
            .AddCheck<StorageHealthCheck>(
                "storage",
                tags: ["ready", "storage"])
            .AddCheck<PacsConnectivityHealthCheck>(
                "pacs",
                tags: ["ready", "pacs"]);

        // OpenTelemetry (conditional)
        var options = diagnosticsSection.Get<DiagnosticsOptions>() ?? new DiagnosticsOptions();
        services.AddEdgeOpenTelemetry(options);

        return services;
    }

    /// <summary>
    /// Configures Serilog as the logging provider with Edge Node enrichers,
    /// PHI redaction and configured sinks (file, Seq, HTTP).
    /// Call this on the <see cref="IHostBuilder"/> before building the host.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The host builder for chaining.</returns>
    public static IHostBuilder UseEdgeLogging(
        this IHostBuilder hostBuilder,
        IConfiguration configuration)
    {
        var diagnosticsSection = configuration.GetSection(DiagnosticsOptions.SectionName);
        var options = diagnosticsSection.Get<DiagnosticsOptions>() ?? new DiagnosticsOptions();

        hostBuilder.UseSerilog((context, services, loggerConfig) =>
        {
            ConfigureBaseLogging(loggerConfig, options);
            ConfigureEnrichers(loggerConfig, services, options);
            ConfigureSinks(loggerConfig, options);

            // Allow overrides from appsettings configuration
            loggerConfig.ReadFrom.Configuration(context.Configuration);
        });

        return hostBuilder;
    }

    /// <summary>
    /// Configures Serilog as the logging provider for <see cref="HostApplicationBuilder"/>
    /// (used in .NET 8+ minimal hosting and Worker Services).
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static HostApplicationBuilder UseEdgeLogging(
        this HostApplicationBuilder builder,
        IConfiguration configuration)
    {
        var diagnosticsSection = configuration.GetSection(DiagnosticsOptions.SectionName);
        var options = diagnosticsSection.Get<DiagnosticsOptions>() ?? new DiagnosticsOptions();

        builder.Services.AddSerilog((services, loggerConfig) =>
        {
            ConfigureBaseLogging(loggerConfig, options);
            ConfigureEnrichers(loggerConfig, services, options);
            ConfigureSinks(loggerConfig, options);

            loggerConfig.ReadFrom.Configuration(configuration);
        });

        return builder;
    }

    private static void ConfigureBaseLogging(
        LoggerConfiguration loggerConfig,
        DiagnosticsOptions options)
    {
        var minimumLevel = options.Environment.Equals("Development", StringComparison.OrdinalIgnoreCase)
            ? LogEventLevel.Debug
            : LogEventLevel.Information;

        loggerConfig
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning);
    }

    private static void ConfigureEnrichers(
        LoggerConfiguration loggerConfig,
        IServiceProvider services,
        DiagnosticsOptions options)
    {
        loggerConfig
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .Enrich.With(new NodeEnricher(
                Microsoft.Extensions.Options.Options.Create(options)))
            .Enrich.With(new CorrelationIdEnricher());

        // PHI redaction enricher — resolve from DI if available,
        // otherwise create with default options
        var protector = services.GetService<IPhiProtector>()
                        ?? new RegexPhiProtector(Microsoft.Extensions.Options.Options.Create(options.Redaction));

        loggerConfig.Enrich.With(new PhiRedactionEnricher(
            protector,
            Microsoft.Extensions.Options.Options.Create(options.Redaction)));
    }

    private static void ConfigureSinks(
        LoggerConfiguration loggerConfig,
        DiagnosticsOptions options)
    {
        var fileOptions = options.File;

        // File sink — always enabled as durable offline fallback
        var filePath = Path.Combine(fileOptions.Path, fileOptions.FileNameTemplate);
        var rollingInterval = Enum.TryParse<Serilog.RollingInterval>(
            fileOptions.RollingInterval, true, out var ri) ? ri : Serilog.RollingInterval.Day;

        if (fileOptions.UseCompactJson)
        {
            loggerConfig.WriteTo.Async(a =>
                a.File(
                    formatter: new CompactJsonFormatter(),
                    path: filePath,
                    rollingInterval: rollingInterval,
                    fileSizeLimitBytes: fileOptions.MaxFileSizeMb * 1024L * 1024L,
                    retainedFileCountLimit: fileOptions.RetainedFileCountLimit,
                    shared: false,
                    buffered: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(2)));
        }
        else
        {
            loggerConfig.WriteTo.Async(a =>
                a.File(
                    path: filePath,
                    rollingInterval: rollingInterval,
                    fileSizeLimitBytes: fileOptions.MaxFileSizeMb * 1024L * 1024L,
                    retainedFileCountLimit: fileOptions.RetainedFileCountLimit,
                    shared: false,
                    buffered: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(2),
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] " +
                                    "[{Level:u3}] " +
                                    "[{NodeId}] " +
                                    "[{CorrelationId}] " +
                                    "{Message:lj}{NewLine}{Exception}"));
        }

        // Seq sink — optional
        var seqOptions = options.Seq;
        if (seqOptions.Enabled && !string.IsNullOrWhiteSpace(seqOptions.Url))
        {
            loggerConfig.WriteTo.Async(a =>
                a.Seq(seqOptions.Url, apiKey: seqOptions.ApiKey));
        }

        // HTTP sink — optional
        var httpOptions = options.HttpSink;
        if (httpOptions.Enabled && !string.IsNullOrWhiteSpace(httpOptions.Url))
        {
            loggerConfig.WriteTo.Async(a =>
                a.Http(
                    requestUri: httpOptions.Url,
                    queueLimitBytes: null));
        }

        // Console sink — development only
        if (options.Environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                                "[{NodeId}] [{Component}] " +
                                "{Message:lj}{NewLine}{Exception}");
        }
    }
}
