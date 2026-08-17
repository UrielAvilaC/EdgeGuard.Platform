using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Dicom.Edge.Diagnostics.Extensions;

/// <summary>
/// Extension methods for configuring diagnostics middleware and endpoints
/// on the ASP.NET Core application pipeline. Shared by Hub and Edge Node.
/// </summary>
public static class PlatformAppBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Adds the correlation ID middleware to the request pipeline.
    /// Should be registered early in the pipeline.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Adds global exception handling middleware that captures unhandled exceptions,
    /// logs them with full context, and returns standardized error responses.
    /// </summary>
    public static IApplicationBuilder UsePlatformExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    /// <summary>
    /// Maps health check endpoints with structured JSON output:
    /// <list type="bullet">
    ///   <item><c>/health/live</c> — liveness probe (always healthy if app is running)</item>
    ///   <item><c>/health/ready</c> — readiness probe (checks storage, DB, etc.)</item>
    /// </list>
    /// </summary>
    public static WebApplication MapDiagnosticsEndpoints(this WebApplication app)
    {
        // Health probes must never be throttled — Kubernetes/load-balancer liveness
        // and readiness checks poll frequently and must bypass any rate limiter.
        app.MapHealthChecks(HealthCheckConstants.LivenessEndpoint, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponse
        }).DisableRateLimiting();

        app.MapHealthChecks(HealthCheckConstants.ReadinessEndpoint, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckConstants.ReadyTag),
            ResponseWriter = WriteResponse
        }).DisableRateLimiting();

        return app;
    }

    private static async Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = HealthCheckConstants.JsonContentType;

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions));
    }
}
