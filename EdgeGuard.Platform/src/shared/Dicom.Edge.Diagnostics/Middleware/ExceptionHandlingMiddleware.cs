using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Dicom.Edge.Diagnostics.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions,
/// logs them with full correlation context, and returns a standardized
/// error response. Prevents stack trace leakage in production.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request {Method} {Path} was cancelled by client",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = MiddlewareConstants.ClientClosedRequestStatusCode;
        }
        catch (Exception ex)
        {
            var correlationId = CorrelationScope.CurrentCorrelationId ?? MiddlewareConstants.UnknownCorrelationId;

            _logger.LogError(ex,
                "Unhandled exception processing {Method} {Path} [CorrelationId: {CorrelationId}]",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = HealthCheckConstants.JsonContentType;

                var response = new
                {
                    error = MiddlewareConstants.InternalErrorMessage,
                    correlationId,
                    timestamp = DateTimeOffset.UtcNow
                };

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response, JsonOptions));
            }
        }
    }
}
