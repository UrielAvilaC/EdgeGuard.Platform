using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Security.Authentication;

/// <summary>
/// ASP.NET Core authentication handler that validates the X-Api-Key header
/// for M2M authentication between Edge Nodes and the Hub.
/// Delegates actual key validation to <see cref="IApiKeyValidator"/>.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IApiKeyValidator apiKeyValidator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var apiKeyHeader))
            return AuthenticateResult.NoResult();

        var apiKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.Fail("API key header is empty.");

        var result = await apiKeyValidator.ValidateAsync(apiKey, Context.RequestAborted);

        if (result is null)
        {
            Logger.LogWarning("Invalid API key presented from {RemoteIp}", Context.Connection.RemoteIpAddress);
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (!result.IsEnabled)
        {
            Logger.LogWarning("API key valid but node {NodeId} is disabled", result.NodeId);
            return AuthenticateResult.Fail("Node is disabled.");
        }

        var claims = new[]
        {
            new Claim("nodeId", result.NodeId),
            new Claim("nodeName", result.NodeName),
            new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationOptions.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.Scheme);

        Logger.LogDebug("Node {NodeId} authenticated via API key", result.NodeId);
        return AuthenticateResult.Success(ticket);
    }
}
