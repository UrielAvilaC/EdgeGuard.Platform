using Dicom.Edge.Node.Diagnostics.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Dicom.Edge.Node.Diagnostics.Observability;

/// <summary>
/// Configures OpenTelemetry tracing and metrics for the Edge Node.
/// </summary>
internal static class OpenTelemetrySetup
{
    public static IServiceCollection AddEdgeOpenTelemetry(
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
                    [LoggingConstants.NodeId] = options.NodeId,
                    [LoggingConstants.Environment] = options.Environment
                });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(LoggingConstants.ActivitySourceName)
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
                    .AddMeter(LoggingConstants.MeterName)
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
