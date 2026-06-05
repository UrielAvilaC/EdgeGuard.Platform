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
    public string? Modality { get; init; }
    public string? ProcedureDescription { get; init; }
    public string? ProcedureId { get; init; }
    public int Priority { get; init; } = 5;

    // ── MWL-FIX-3 — fields required by the Edge Node C-FIND SCP ──────────
    public string? PatientBirthDate { get; init; }
    public string? PatientSex { get; init; }
    public DateTime? ScheduledDateTime { get; init; }
    public string? ScheduledStationAeTitle { get; init; }
    public string? ScheduledPerformingPhysicianName { get; init; }
    public string? ScheduledProcedureStepId { get; init; }
    public string? ReferringPhysicianName { get; init; }
    public string? StudyInstanceUid { get; init; }
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
