namespace Dicom.Edge.Contracts.Node;

/// <summary>
/// Response returned by the Edge Node health endpoint (GET /api/health).
/// Used by the Hub to verify node reachability and basic status.
/// </summary>
public sealed class NodeHealthResponse
{
    public required string Status { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required string NodeName { get; init; }
    public string? Version { get; init; }
    public int ActiveWorklistItems { get; init; }
    public bool DicomServerRunning { get; init; }
}
