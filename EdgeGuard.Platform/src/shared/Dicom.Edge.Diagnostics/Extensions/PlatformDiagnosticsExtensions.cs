using Dicom.Edge.Abstractions.Audit;
using Dicom.Edge.Diagnostics.Audit;
using Dicom.Edge.Diagnostics.Configuration;
using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.Enrichers;
using Dicom.Edge.Diagnostics.HealthChecks;
using Dicom.Edge.Diagnostics.Observability;
using Dicom.Edge.Diagnostics.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Dicom.Edge.Diagnostics.Extensions;

/// <summary>
/// Extension methods for registering platform diagnostics services
/// including logging, health checks, metrics, PHI redaction and OpenTelemetry.
/// Shared by Hub and Edge Node — each adds platform-specific extras on top.
/// </summary>
public static class PlatformDiagnosticsExtensions
{
    /// <summary>
    /// Registers all platform diagnostics services into the DI container:
    /// options, PHI redaction, health checks, audit logger, activity source and OpenTelemetry.
    /// </summary>
    public static IServiceCollection AddPlatformDiagnostics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var diagnosticsSection = configuration.GetSection(DiagnosticsOptions.SectionName);
        services.Configure<DiagnosticsOptions>(diagnosticsSection);
        services.Configure<PhiRedactionOptions>(diagnosticsSection.GetSection("Redaction"));
        services.Configure<FileLoggingOptions>(diagnosticsSection.GetSection("File"));
        services.Configure<HealthCheckThresholdOptions>(diagnosticsSection.GetSection("HealthChecks"));

        // Configuration validation — fail-fast on invalid settings
        services.AddSingleton<IValidateOptions<DiagnosticsOptions>, DiagnosticsOptionsValidator>();

        // PHI redaction
        services.AddSingleton<IPhiProtector, RegexPhiProtector>();

        // Audit logging (HIPAA/GDPR compliance)
        services.AddSingleton<IAuditLogger, StructuredAuditLogger>();

        // Activity source for distributed tracing
        var options = diagnosticsSection.Get<DiagnosticsOptions>() ?? new DiagnosticsOptions();
        services.AddSingleton(new PlatformActivitySource(options.ActivitySourceName));

        // Storage health check (generic)
        services.AddHealthChecks()
            .AddCheck<StorageHealthCheck>(
                HealthCheckConstants.StorageCheckName,
                tags: [HealthCheckConstants.ReadyTag, HealthCheckConstants.StorageTag]);

        // OpenTelemetry (conditional)
        services.AddPlatformOpenTelemetry(options);

        return services;
    }

    /// <summary>
    /// Configures Serilog as the logging provider for <see cref="HostApplicationBuilder"/>
    /// (used in .NET 8+ minimal hosting and Worker Services).
    /// </summary>
    public static HostApplicationBuilder UsePlatformLogging(
        this HostApplicationBuilder builder,
        IConfiguration? configuration = null)
    {
        configuration ??= builder.Configuration;
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

    /// <summary>
    /// Configures Serilog as the logging provider on <see cref="IHostBuilder"/>.
    /// Compatible with WebApplicationBuilder via <c>builder.Host.UsePlatformLogging(...)</c>.
    /// </summary>
    public static IHostBuilder UsePlatformLogging(
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

            loggerConfig.ReadFrom.Configuration(context.Configuration);
        });

        return hostBuilder;
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
            .Enrich.With(new InstanceEnricher(
                Microsoft.Extensions.Options.Options.Create(options)))
            .Enrich.With(new CorrelationIdEnricher());

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

        var logDirectory = Path.GetFullPath(fileOptions.Path);
        Directory.CreateDirectory(logDirectory);

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
                                    "[{InstanceId}] " +
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
                                "[{InstanceId}] [{Component}] " +
                                "{Message:lj}{NewLine}{Exception}");
        }
    }
}
