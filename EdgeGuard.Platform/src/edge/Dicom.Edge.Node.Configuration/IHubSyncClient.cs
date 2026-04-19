namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Communicates with the Hub to register, send heartbeats, and pull configuration.
/// </summary>
public interface IHubSyncClient
{
    /// <summary>
    /// Registers with the Hub. Returns the API key on first registration, null on re-registration or failure.
    /// </summary>
    Task<string?> RegisterAsync(CancellationToken ct = default);
    Task<bool> SendHeartbeatAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>?> PullConfigurationAsync(CancellationToken ct = default);
    Task<bool> DeregisterAsync(CancellationToken ct = default);
}
