namespace Dicom.Edge.Hub.Infrastructure.Http;

/// <summary>
/// P0-1: Resolves the per-node API key used by <see cref="HubAuthDelegatingHandler"/>
/// to sign outbound Hub→Node requests.
/// </summary>
public interface INodeAuthKeyProvider
{
    /// <summary>
    /// Returns the API key for the given node id, or <c>null</c> when the node has not
    /// been issued a key yet (legacy / pre-bootstrap).
    /// </summary>
    Task<string?> GetApiKeyAsync(string nodeId, CancellationToken ct = default);
}
