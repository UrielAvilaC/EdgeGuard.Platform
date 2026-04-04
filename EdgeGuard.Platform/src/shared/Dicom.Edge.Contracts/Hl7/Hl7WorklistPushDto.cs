namespace Dicom.Edge.Contracts.Hl7;

/// <summary>
/// Payload sent by the Hub to an Edge Node when dispatching an HL7 worklist item.
/// </summary>
public sealed class Hl7WorklistPushRequest
{
    public required Guid HubMessageId { get; init; }
    public required string MessageType { get; init; }
    public required string? TriggerEvent { get; init; }
    public required string RawContent { get; init; }

    // Parsed fields for direct access
    public string? PatientId { get; init; }
    public string? PatientName { get; init; }
    public string? AccessionNumber { get; init; }
    public string? SendingFacility { get; init; }
    public string? SendingApplication { get; init; }

    public int Priority { get; init; } = 5;
    public DateTime SentAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Acknowledgment returned by the Edge Node after receiving an HL7 worklist push.
/// </summary>
public sealed class Hl7WorklistPushResponse
{
    public required bool Accepted { get; init; }
    public string? NodeAckId { get; init; }
    public string? Error { get; init; }
    public DateTime ReceivedAtUtc { get; init; } = DateTime.UtcNow;

    public static Hl7WorklistPushResponse Accept(string ackId) =>
        new() { Accepted = true, NodeAckId = ackId };

    public static Hl7WorklistPushResponse Reject(string error) =>
        new() { Accepted = false, Error = error };
}
