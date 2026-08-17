using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Http;

/// <summary>
/// P0-1: Resolves the raw signing key per node, with short-TTL in-memory caching
/// so the DB is not hit on every outbound push.
///
/// <para>The key is <see cref="Node.SigningSecret"/> — the node's API key held in reversible
/// form, encrypted at rest — decrypted here for <see cref="HubAuthDelegatingHandler"/> to HMAC
/// the outbound request. <c>ApiKeyHash</c> cannot serve this purpose: it is one-way and only
/// verifies inbound node→Hub calls.</para>
///
/// <para>Nodes registered before the field existed have no secret until they next authenticate
/// against the Hub (<c>NodeApiKeyValidator</c> backfills it). Until then this returns
/// <c>null</c> and the request goes out unsigned, which the node still accepts while
/// <c>NodeAuth:Enforce = false</c>.</para>
/// </summary>
public sealed class NodeAuthKeyProvider : INodeAuthKeyProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly INodeRepository _nodes;
    private readonly ISettingEncryptionService _secretProtector;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NodeAuthKeyProvider> _logger;

    public NodeAuthKeyProvider(
        INodeRepository nodes,
        ISettingEncryptionService secretProtector,
        IMemoryCache cache,
        ILogger<NodeAuthKeyProvider> logger)
    {
        _nodes = nodes;
        _secretProtector = secretProtector;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetApiKeyAsync(string nodeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return null;

        var cacheKey = $"node-signing-key:{nodeId}";
        if (_cache.TryGetValue<string>(cacheKey, out var cached))
            return cached;

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node is null)
        {
            _logger.LogWarning("Outbound auth lookup: node {NodeId} not found", nodeId);
            return null;
        }

        string? signingKey = null;

        if (node.SigningSecret is { Length: > 0 } encrypted)
        {
            try
            {
                signingKey = _secretProtector.Decrypt(encrypted);
            }
            catch (Exception ex)
            {
                // Typically a Data Protection key ring that was rotated away or lost. The
                // stored ciphertext is unrecoverable; the node re-supplies its key on its next
                // authenticated call and NodeApiKeyValidator writes a fresh secret.
                _logger.LogError(ex,
                    "Could not decrypt the signing secret for node {NodeId} — sending unsigned",
                    nodeId);
            }
        }
        else
        {
            _logger.LogDebug(
                "Node {NodeId} has no signing secret yet (pre-existing registration) — sending unsigned",
                nodeId);
        }

        _cache.Set(cacheKey, signingKey, CacheTtl);
        return signingKey;
    }
}
