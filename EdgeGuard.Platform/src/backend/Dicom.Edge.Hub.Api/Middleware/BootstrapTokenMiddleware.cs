using Dicom.Edge.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Api.Middleware;

/// <summary>
/// Middleware that validates the bootstrap token for the /edge/register endpoint.
/// The bootstrap token is read from the environment variable EDGE_BOOTSTRAP_TOKEN.
/// All other /edge/* endpoints are protected by API key authentication.
/// </summary>
public sealed class BootstrapTokenMiddleware(
    RequestDelegate next,
    ILogger<BootstrapTokenMiddleware> logger)
{
    private static readonly string? BootstrapToken =
        Environment.GetEnvironmentVariable(ApiKeyAuthenticationOptions.BootstrapTokenEnvVar);

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // Only intercept POST /edge/register
        if (path is "/edge/register" && HttpMethods.IsPost(context.Request.Method))
        {
            if (string.IsNullOrEmpty(BootstrapToken))
            {
                logger.LogError(
                    "Bootstrap token not configured. Set environment variable {EnvVar} to enable node registration.",
                    ApiKeyAuthenticationOptions.BootstrapTokenEnvVar);

                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { error = "Node registration is not configured." });
                return;
            }

            if (!context.Request.Headers.TryGetValue(
                    ApiKeyAuthenticationOptions.BootstrapHeaderName, out var tokenHeader)
                || tokenHeader.ToString() != BootstrapToken)
            {
                logger.LogWarning(
                    "Invalid or missing bootstrap token from {RemoteIp}",
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid bootstrap token." });
                return;
            }

            logger.LogInformation("Bootstrap token validated for registration from {RemoteIp}",
                context.Connection.RemoteIpAddress);
        }

        await next(context);
    }
}
