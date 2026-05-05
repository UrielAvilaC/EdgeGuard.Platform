using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Communicates with the Hub to register, send heartbeats, and pull configuration.
/// </summary>
public interface IHubSyncClient
{
    /// <summary>Node ID assigned by the Hub after successful registration. Null until registered.</summary>
    string? RegisteredNodeId { get; }

    /// <summary>
    /// Registers with the Hub.
    /// Returns <see cref="RegistrationResult"/> indicating success and whether a new API key was issued.
    /// </summary>
    Task<RegistrationResult> RegisterAsync(CancellationToken ct = default);
    Task<bool> SendHeartbeatAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>?> PullConfigurationAsync(CancellationToken ct = default);
    Task<bool> DeregisterAsync(CancellationToken ct = default);

    /// <summary>Sends a periodic telemetry snapshot to the Hub.</summary>
    Task<bool> SendTelemetryAsync(NodeTelemetryRequest request, CancellationToken ct = default);
}

/// <param name="Success">True if registration was accepted (first or re-registration).</param>
/// <param name="NewApiKey">Non-null only on first registration; null on re-registration.</param>
/// <param name="NodeId">NodeId returned by the Hub.</param>
public record RegistrationResult(bool Success, string? NewApiKey, string? NodeId);
