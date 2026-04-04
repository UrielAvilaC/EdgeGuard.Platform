namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Communicates with the Hub to register, send heartbeats, and pull configuration.
/// </summary>
public interface IHubSyncClient
{
    Task<bool> RegisterAsync(CancellationToken ct = default);
    Task<bool> SendHeartbeatAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>?> PullConfigurationAsync(CancellationToken ct = default);
    Task<bool> DeregisterAsync(CancellationToken ct = default);
}
