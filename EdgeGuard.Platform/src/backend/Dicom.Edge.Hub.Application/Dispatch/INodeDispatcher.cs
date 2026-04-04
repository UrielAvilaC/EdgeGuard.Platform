namespace Dicom.Edge.Hub.Application.Dispatch;

/// <summary>
/// Sends HL7 worklist data to an Edge Node via HTTP/JSON.
/// </summary>
public interface INodeDispatcher
{
    Task<NodeDispatchResult> DispatchAsync(NodeDispatchRequest request, CancellationToken ct = default);
}

public sealed class NodeDispatchRequest
{
    public required Guid MessageId { get; init; }
    public required string TargetNodeId { get; init; }
    public required string NodeApiEndpoint { get; init; }
    public required string MessageType { get; init; }
    public required string? TriggerEvent { get; init; }
    public required string Content { get; init; }
    public required string? PatientId { get; init; }
    public required string? PatientName { get; init; }
    public required string? AccessionNumber { get; init; }
    public required string? SendingFacility { get; init; }
    public required string? SendingApplication { get; init; }
    public int Priority { get; init; } = 5;
}

public sealed class NodeDispatchResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? NodeAckId { get; init; }

    public static NodeDispatchResult Ok(string? ackId = null) =>
        new() { Success = true, NodeAckId = ackId };

    public static NodeDispatchResult Fail(string error) =>
        new() { Success = false, Error = error };
}
