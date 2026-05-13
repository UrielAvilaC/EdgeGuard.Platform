using Dicom.Edge.Diagnostics.Configuration;
using Dicom.Edge.Diagnostics.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Diagnostics.Middleware;

/// <summary>
/// ASP.NET Core middleware that extracts or generates a correlation ID
/// for each request, propagates it to <see cref="CorrelationScope"/>
/// and echoes it back in the response headers.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerName;

    public CorrelationIdMiddleware(RequestDelegate next, IOptions<DiagnosticsOptions> options)
    {
        _next = next;
        _headerName = options.Value.CorrelationIdHeader;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ExtractOrGenerate(context);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_headerName] = correlationId;
            return Task.CompletedTask;
        });

        using (CorrelationScope.Start(correlationId))
        {
            await _next(context);
        }
    }

    private string ExtractOrGenerate(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_headerName, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            return existing.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
