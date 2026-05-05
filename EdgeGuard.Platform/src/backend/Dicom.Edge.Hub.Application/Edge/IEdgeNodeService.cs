using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Hub.Application.Edge;

/// <summary>
/// Application service for Edge (node-facing) write operations.
/// Encapsulates the business orchestration previously embedded in EdgeController:
/// registration, heartbeat processing, study notification, and health report ingestion.
/// </summary>
public interface IEdgeNodeService
{
    /// <summary>Registers a node (or re-registers if AeTitle exists).</summary>
    Task<NodeRegistrationResponse> RegisterAsync(NodeRegistrationRequest request, CancellationToken ct = default);

    /// <summary>Processes a heartbeat from a node. Returns null if node not found.</summary>
    Task<EdgeOperationResult?> ProcessHeartbeatAsync(NodeHeartbeatRequest request, CancellationToken ct = default);

    /// <summary>Processes a study notification from a node. Returns null if node not found.</summary>
    Task<EdgeStudyNotifyResult?> ProcessStudyNotifyAsync(StudyNotifyRequest request, CancellationToken ct = default);

    /// <summary>Processes a health report from a node. Returns null if node not found.</summary>
    Task<EdgeOperationResult?> ProcessHealthReportAsync(NodeHealthReportRequest request, CancellationToken ct = default);

    /// <summary>Persists a telemetry snapshot from a node. Returns null if node not found.</summary>
    Task<EdgeOperationResult?> ProcessTelemetryAsync(NodeTelemetryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns the configuration dictionary for a node, or null if node not found.
    /// Empty string nodeId returns null.
    /// </summary>
    Task<Dictionary<string, string>?> PullConfigurationAsync(string nodeId, CancellationToken ct = default);
}

/// <summary>
/// Result of an edge operation (heartbeat, health report).
/// </summary>
public sealed record EdgeOperationResult(bool Acknowledged, DateTime ServerTimeUtc);

/// <summary>
/// Result of a study notification from a node.
/// </summary>
public sealed record EdgeStudyNotifyResult(bool Acknowledged, string StudyId, DateTime ReceivedAtUtc);
