using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Http;

/// <summary>
/// P0-1: Resolves the raw signing key per node, with short-TTL in-memory caching
/// so the DB is not hit on every outbound push.
///
/// <para><b>Implementation note:</b> The <see cref="Node"/> aggregate currently stores
/// only <c>ApiKeyHash</c> (one-way hash for verifying INCOMING calls from the node).
/// Outbound signing requires the raw key. Until the Node entity is extended with a
/// (server-side-encrypted) signing-secret column, this provider returns <c>null</c>,
/// which leaves the outbound request unsigned. The Node-side middleware will allow
/// such requests through while <c>NodeAuth:Enforce = false</c> (feature flag), giving
/// us a safe rollout path.</para>
///
/// <para>To complete the loop end-to-end, add a <c>SigningSecret</c> field to
/// <see cref="Node"/>, populate it at registration (returned to the Node once via the
/// bootstrap response), and have this class return it.</para>
/// </summary>
public sealed class NodeAuthKeyProvider : INodeAuthKeyProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly INodeRepository _nodes;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NodeAuthKeyProvider> _logger;

    public NodeAuthKeyProvider(
        INodeRepository nodes,
        IMemoryCache cache,
        ILogger<NodeAuthKeyProvider> logger)
    {
        _nodes = nodes;
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

        // TODO: extend Node aggregate with a raw SigningSecret (encrypted at rest)
        // and return it here. Until then, signing is a no-op and the Node middleware
        // remains in monitor mode (NodeAuth:Enforce=false).
        string? signingKey = null;

        _cache.Set(cacheKey, signingKey, CacheTtl);
        return signingKey;
    }
}
