namespace Dicom.Edge.Node.Queue;

/// <summary>
/// Represents a work item in the node processing pipeline.
/// </summary>
public sealed class NodeWorkItem
{
    public required string Id { get; init; }
    public required string StudyInstanceUid { get; init; }
    public required NodeWorkItemType Type { get; init; }
    public int Priority { get; set; } = 5;
    public string? SourceAeTitle { get; init; }
    public string? TargetPacsId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }

    // ── Study metadata for Hub notification (populated at enqueue time) ────────
    public string? PatientId { get; init; }
    public string? PatientName { get; init; }
    public string? AccessionNumber { get; init; }
    public long TotalSizeBytes { get; init; }
    public int InstanceCount { get; init; }
}

public enum NodeWorkItemType
{
    DicomReceive,
    PacsSend,
    WorklistProcess,
    StudyCleanup
}
