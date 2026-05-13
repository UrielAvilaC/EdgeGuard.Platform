namespace Dicom.Edge.Node.Worklist;

/// <summary>
/// Represents a local worklist item stored on the node.
/// Contains both HL7 source metadata and DICOM MWL-relevant attributes
/// needed to respond to Modality Worklist C-FIND queries.
/// </summary>
public sealed class WorklistItem
{
    // ── Identity / Hub tracking ──────────────────────────────────────────
    public required string Id { get; init; }
    public required Guid HubMessageId { get; init; }
    public required string MessageType { get; init; }

    // ── Patient Level (DICOM 0010,xxxx) ──────────────────────────────────
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

    // ── Scheduled Procedure Step (DICOM 0040,0100) ───────────────────────
    public string? Modality { get; init; }
    public DateTime? ScheduledDateTime { get; init; }
    public string? ScheduledStationAeTitle { get; init; }
    public string? ScheduledPerformingPhysicianName { get; init; }
    public string? ScheduledProcedureStepId { get; init; }

    // ── HL7 source metadata ──────────────────────────────────────────────
    public string? SendingFacility { get; init; }
    public string? RawContent { get; init; }
    public int Priority { get; init; }
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; init; }
    public bool IsProcessed { get; set; }
}
