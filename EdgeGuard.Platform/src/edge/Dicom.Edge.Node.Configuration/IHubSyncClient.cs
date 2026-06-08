using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Communicates with the Hub to register, send heartbeats, and pull configuration.
/// </summary>
public interface IHubSyncClient
{
    /// <summary>Node ID assigned by the Hub after successful registration. Null until registered.</summary>
    string? RegisteredNodeId { get; }
    HubConnectionOptions ConnectionOptions { get; }
    Task<RegistrationResult> RegisterAsync(CancellationToken ct = default);
    Task<bool> SendHeartbeatAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>?> PullConfigurationAsync(CancellationToken ct = default);
    Task<bool> DeregisterAsync(CancellationToken ct = default);

    /// <summary>Sends a periodic telemetry snapshot to the Hub.</summary>
    Task<bool> SendTelemetryAsync(NodeTelemetryRequest request, CancellationToken ct = default);

    /// <summary>Notifies the Hub that a study has completed so it appears in the SPA.</summary>
    Task<bool> NotifyStudyAsync(StudyNotifyRequest request, CancellationToken ct = default);

    /// <summary>Sends incremental study progress updates to the Hub as DICOM instances arrive.</summary>
    Task<bool> NotifyStudyProgressAsync(StudyProgressNotifyRequest request, CancellationToken ct = default);

    /// <summary>Reports PACS C-ECHO results to the Hub so the SPA can display connectivity status.</summary>
    Task<bool> ReportPacsEchoAsync(NodePacsEchoReportRequest request, CancellationToken ct = default);

    /// <summary>Reports recent equipment activity (passive presence) so the Hub can show last-seen / online status.</summary>
    Task<bool> ReportEquipmentStatusAsync(NodeEquipmentStatusReportRequest request, CancellationToken ct = default);
}

/// <param name="Success">True if registration was accepted (first or re-registration).</param>
/// <param name="NewApiKey">Non-null only on first registration; null on re-registration.</param>
/// <param name="NodeId">NodeId returned by the Hub.</param>
public record RegistrationResult(bool Success, string? NewApiKey, string? NodeId);
