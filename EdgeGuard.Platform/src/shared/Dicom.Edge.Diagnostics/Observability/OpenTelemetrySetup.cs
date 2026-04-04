using Dicom.Edge.Diagnostics.Configuration;
using Dicom.Edge.Diagnostics.Constants;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Dicom.Edge.Diagnostics.Observability;

/// <summary>
/// Configures OpenTelemetry tracing and metrics for the platform.
/// Parameterized by <see cref="DiagnosticsOptions"/> for Hub or Node usage.
/// </summary>
internal static class OpenTelemetrySetup
{
    public static IServiceCollection AddPlatformOpenTelemetry(
        this IServiceCollection services,
        DiagnosticsOptions options)
    {
        var otelOptions = options.OpenTelemetry;

        if (!otelOptions.Enabled)
            return services;

        services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(
                    serviceName: otelOptions.ServiceName,
                    serviceVersion: otelOptions.ServiceVersion ?? "1.0.0");
                r.AddAttributes(new Dictionary<string, object>
                {
                    [DiagnosticsConstants.InstanceId] = options.InstanceId,
                    [DiagnosticsConstants.Environment] = options.Environment
                });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(options.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otelOptions.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otelOptions.OtlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(options.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otelOptions.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otelOptions.OtlpEndpoint));
                }
            });

        return services;
    }
}
