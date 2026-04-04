namespace Dicom.Edge.Contracts.Node;

/// <summary>
/// Response returned by the Edge Node worklist count endpoint.
/// </summary>
public sealed class WorklistCountResponse
{
    public required int ActiveItems { get; init; }
    public required DateTime TimestampUtc { get; init; }
}
