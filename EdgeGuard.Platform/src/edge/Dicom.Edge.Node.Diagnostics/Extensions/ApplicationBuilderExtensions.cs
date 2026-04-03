using Dicom.Edge.Node.Diagnostics.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Dicom.Edge.Node.Diagnostics.Extensions;

/// <summary>
/// Extension methods for configuring diagnostics middleware and endpoints
/// on the ASP.NET Core application pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
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
    /// Should be registered immediately after <see cref="UseCorrelationId"/>.
    /// </summary>
    public static IApplicationBuilder UseEdgeExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    /// <summary>
    /// Maps health check endpoints with structured JSON output:
    /// <list type="bullet">
    ///   <item><c>/health/live</c> — liveness probe (always healthy if app is running)</item>
    ///   <item><c>/health/ready</c> — readiness probe (checks storage, PACS, queue)</item>
    /// </list>
    /// </summary>
    public static WebApplication MapDiagnosticsEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // No checks — just confirm process is running
            ResponseWriter = WriteResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteResponse
        });

        return app;
    }

    private static async Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

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
