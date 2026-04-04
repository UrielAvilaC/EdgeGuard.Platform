namespace Dicom.Edge.Node.Processing;

/// <summary>
/// Represents the pipeline processing result for a completed study.
/// </summary>
public sealed class StudyPipelineResult
{
    public required string StudyInstanceUid { get; init; }
    public bool RoutingSuccess { get; init; }
    public int DestinationsSent { get; init; }
    public int DestinationsFailed { get; init; }
    public bool HubNotified { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public List<string> Errors { get; init; } = [];
}
