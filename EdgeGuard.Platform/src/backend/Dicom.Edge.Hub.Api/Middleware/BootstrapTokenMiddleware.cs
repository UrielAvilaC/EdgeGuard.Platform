using Dicom.Edge.Hub.Application.Edge;
using Dicom.Edge.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Api.Middleware;

/// <summary>
/// Validates the bootstrap token for POST /edge/register.
/// Tokens are one-time use, stored as SHA-256 hashes in the database.
/// Generated via POST /api/nodes/bootstrap-tokens (admin endpoint).
/// </summary>
public sealed class BootstrapTokenMiddleware(
    RequestDelegate next,
    ILogger<BootstrapTokenMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Value is "/edge/register"
            && HttpMethods.IsPost(context.Request.Method))
        {
            if (!context.Request.Headers.TryGetValue(
                    ApiKeyAuthenticationOptions.BootstrapHeaderName, out var tokenHeader)
                || string.IsNullOrWhiteSpace(tokenHeader))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Bootstrap token required." });
                return;
            }

            var tokenService = context.RequestServices
                .GetRequiredService<IBootstrapTokenService>();

            var token = await tokenService.ValidateAsync(tokenHeader.ToString(), context.RequestAborted);

            if (token is null)
            {
                logger.LogWarning(
                    "Invalid or expired bootstrap token from {RemoteIp}",
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired bootstrap token." });
                return;
            }

            // Store validated token so EdgeController can consume it after registration
            context.Items["ValidatedBootstrapToken"] = token;

            logger.LogInformation(
                "Bootstrap token {TokenId} validated from {RemoteIp}",
                token.Id, context.Connection.RemoteIpAddress);
        }

        await next(context);
    }
}
