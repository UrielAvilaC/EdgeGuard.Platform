using System.Net;
using System.Security.Cryptography;
using System.Text;
using Dicom.Edge.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Api.Middleware;

/// <summary>
/// P0-1: Validates HMAC-signed Hub→Node requests on configuration endpoints.
///
/// Hub signs every request with HMAC-SHA256 over <c>timestamp + bodyHash</c> using
/// the per-node API key. The node:
/// <list type="number">
///   <item>Reads <c>Authorization: Bearer &lt;ApiKey&gt;</c> and validates against the local stored ApiKey.</item>
///   <item>Reads <c>X-Hub-Timestamp</c> and rejects values outside the ±5 minute window (anti-replay).</item>
///   <item>Recomputes the HMAC and compares with <c>X-Hub-Signature</c> using timing-safe compare.</item>
/// </list>
/// Only paths under the protected prefixes are checked; others (e.g. <c>/health</c>) pass through.
///
/// <para><b>Feature flag:</b> <c>NodeAuth:Enforce</c> (default <c>false</c>). When <c>false</c>,
/// invalid/missing signatures are logged but the request is allowed through — safe rollout.</para>
/// </summary>
public sealed class HubAuthenticationMiddleware
{
    private const string ApiKeySettingKey = "hub.api_key";
    private const int    MaxClockSkewSeconds = 300;

    private static readonly string[] ProtectedPathPrefixes =
    [
        "/api/dicom-routing-rules",
        "/api/pacs-destinations",
        "/api/configuration",
        "/api/studies",
    ];

    private readonly RequestDelegate _next;
    private readonly bool _enforce;
    private readonly ILogger<HubAuthenticationMiddleware> _logger;

    public HubAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<HubAuthenticationMiddleware> logger)
    {
        _next = next;
        _enforce = configuration.GetValue("NodeAuth:Enforce", defaultValue: false);
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, INodeSettingsService settings)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var requiresAuth = ProtectedPathPrefixes
            .Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!requiresAuth)
        {
            await _next(context);
            return;
        }

        var validation = await ValidateAsync(context, settings);
        if (!validation.IsValid)
        {
            if (_enforce)
            {
                _logger.LogWarning(
                    "Rejected unauthenticated Hub request to {Path} ({Reason})",
                    path, validation.Reason);
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    $"{{\"error\":\"unauthorized\",\"reason\":\"{validation.Reason}\"}}");
                return;
            }

            _logger.LogWarning(
                "Hub request to {Path} failed auth ({Reason}) — allowing (NodeAuth:Enforce=false)",
                path, validation.Reason);
        }

        await _next(context);
    }

    private async Task<AuthResult> ValidateAsync(HttpContext ctx, INodeSettingsService settings)
    {
        // 1. Bearer header
        var authHeader = ctx.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthResult.Fail("missing-bearer");

        var presentedKey = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(presentedKey))
            return AuthResult.Fail("empty-bearer");

        // 2. Compare with locally stored key
        var storedKey = await settings.GetAsync<string?>(ApiKeySettingKey, null, ctx.RequestAborted);
        if (string.IsNullOrEmpty(storedKey))
            return AuthResult.Fail("no-local-key");
        if (!FixedTimeEquals(presentedKey, storedKey))
            return AuthResult.Fail("api-key-mismatch");

        // 3. Timestamp ± skew
        var tsHeader = ctx.Request.Headers["X-Hub-Timestamp"].ToString();
        if (!long.TryParse(tsHeader, out var unixTs))
            return AuthResult.Fail("missing-timestamp");
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - unixTs) > MaxClockSkewSeconds)
            return AuthResult.Fail("timestamp-out-of-window");

        // 4. Recompute HMAC over (timestamp + bodyHash)
        var sigHeader = ctx.Request.Headers["X-Hub-Signature"].ToString();
        if (string.IsNullOrEmpty(sigHeader))
            return AuthResult.Fail("missing-signature");

        ctx.Request.EnableBuffering();
        ctx.Request.Body.Position = 0;
        var bodyBytes = await ReadAllBytesAsync(ctx.Request.Body, ctx.RequestAborted);
        ctx.Request.Body.Position = 0;

        var bodyHash    = Convert.ToHexString(SHA256.HashData(bodyBytes)).ToLowerInvariant();
        var payload     = $"{unixTs}.{bodyHash}";
        using var hmac  = new HMACSHA256(Encoding.UTF8.GetBytes(storedKey));
        var computedSig = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        if (!FixedTimeEquals(computedSig, sigHeader.Trim().ToLowerInvariant()))
            return AuthResult.Fail("signature-mismatch");

        return AuthResult.Ok();
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream s, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        if (aBytes.Length != bBytes.Length) return false;
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private readonly record struct AuthResult(bool IsValid, string Reason)
    {
        public static AuthResult Ok() => new(true, string.Empty);
        public static AuthResult Fail(string reason) => new(false, reason);
    }
}
