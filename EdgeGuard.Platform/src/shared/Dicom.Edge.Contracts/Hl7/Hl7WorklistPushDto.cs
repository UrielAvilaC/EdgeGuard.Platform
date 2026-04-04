namespace Dicom.Edge.Contracts.Hl7;

/// <summary>
/// Payload sent by the Hub to an Edge Node when dispatching an HL7 worklist item.
/// Contains both raw HL7 content and pre-parsed MWL-relevant fields for direct use
/// by the DICOM Modality Worklist SCP.
/// </summary>
public sealed class Hl7WorklistPushRequest
{
    public required Guid HubMessageId { get; init; }
    public required string MessageType { get; init; }
    public required string? TriggerEvent { get; init; }
    public required string RawContent { get; init; }

    // ── Patient-level fields ─────────────────────────────────────────────
    public string? PatientId { get; init; }
    public string? PatientName { get; init; }
    public string? PatientBirthDate { get; init; }
    public string? PatientSex { get; init; }

    // ── Study / Requested Procedure ──────────────────────────────────────
    public string? AccessionNumber { get; init; }
    public string? StudyInstanceUid { get; init; }
    public string? ReferringPhysicianName { get; init; }
    public string? ProcedureDescription { get; init; }
    public string? RequestedProcedureId { get; init; }

    // ── Scheduled Procedure Step ─────────────────────────────────────────
    public string? Modality { get; init; }
    public DateTime? ScheduledDateTime { get; init; }
    public string? ScheduledStationAeTitle { get; init; }
    public string? ScheduledPerformingPhysicianName { get; init; }
    public string? ScheduledProcedureStepId { get; init; }

    // ── HL7 routing metadata ─────────────────────────────────────────────
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
