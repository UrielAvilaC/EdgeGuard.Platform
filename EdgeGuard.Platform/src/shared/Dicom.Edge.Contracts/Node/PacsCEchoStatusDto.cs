namespace Dicom.Edge.Contracts.Node;

/// <summary>
/// Result of a periodic PACS C-ECHO verification.
/// Returned by the PACS status endpoint on the Edge Node.
/// </summary>
public sealed class PacsCEchoResultDto
{
    public required string DestinationAeTitle { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required bool Success { get; init; }
    public required DateTime CheckedAtUtc { get; init; }
    public double? LatencyMs { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Aggregated PACS C-ECHO status response for all configured destinations.
/// </summary>
public sealed class PacsCEchoStatusResponse
{
    public required IReadOnlyList<PacsCEchoResultDto> Destinations { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public int TotalChecked => Destinations.Count;
    public int TotalReachable => Destinations.Count(d => d.Success);
}
