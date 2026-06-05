using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Http;

/// <summary>
/// P0-1: Signs outbound Hub→Node requests with HMAC-SHA256 so the Node can verify
/// the request originated from the Hub.
///
/// <para>The signature is computed over <c>"{unixTimestamp}.{sha256HexOfBody}"</c>
/// using the per-node API key. Headers added:
/// <list type="bullet">
///   <item><c>Authorization: Bearer {ApiKey}</c></item>
///   <item><c>X-Hub-Timestamp: {unixSeconds}</c></item>
///   <item><c>X-Hub-Signature: {hexSha256Hmac}</c></item>
/// </list>
/// </para>
///
/// <para>The target node id MUST be present in the <c>X-Node-Id</c> request header so
/// this handler can look up the right API key. The caller (push service) is responsible
/// for setting that header before passing the request to <c>HttpClient.SendAsync</c>.</para>
/// </summary>
public sealed class HubAuthDelegatingHandler : DelegatingHandler
{
    private readonly INodeAuthKeyProvider _keyProvider;
    private readonly ILogger<HubAuthDelegatingHandler> _logger;

    public HubAuthDelegatingHandler(
        INodeAuthKeyProvider keyProvider,
        ILogger<HubAuthDelegatingHandler> logger)
    {
        _keyProvider = keyProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var nodeId = request.Headers.TryGetValues("X-Node-Id", out var values)
            ? values.FirstOrDefault()
            : null;

        if (string.IsNullOrEmpty(nodeId))
        {
            _logger.LogWarning(
                "Outbound request to {Url} has no X-Node-Id header — sending unsigned (legacy)",
                request.RequestUri);
            return await base.SendAsync(request, ct);
        }

        var apiKey = await _keyProvider.GetApiKeyAsync(nodeId, ct);
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning(
                "No API key registered for node {NodeId} — sending unsigned (legacy bootstrap)",
                nodeId);
            return await base.SendAsync(request, ct);
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var bodyBytes = request.Content is null
            ? Array.Empty<byte>()
            : await request.Content.ReadAsByteArrayAsync(ct);
        var bodyHashHex = Convert.ToHexString(SHA256.HashData(bodyBytes)).ToLowerInvariant();

        var payload = $"{timestamp}.{bodyHashHex}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey));
        var sigHex = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Remove("X-Hub-Timestamp");
        request.Headers.Add("X-Hub-Timestamp", timestamp);
        request.Headers.Remove("X-Hub-Signature");
        request.Headers.Add("X-Hub-Signature", sigHex);

        return await base.SendAsync(request, ct);
    }
}
