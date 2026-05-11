using Dicom.Edge.Diagnostics.Constants;

namespace Dicom.Edge.Hub.Diagnostics.Constants;

/// <summary>
/// Hub-specific structured logging property names.
/// Extends <see cref="DiagnosticsConstants"/> with HL7 pipeline and dispatch context.
/// </summary>
public static class HubDiagnosticsConstants
{
    // ==================== HL7 Pipeline ====================

    /// <summary>HL7 message unique identifier.</summary>
    public const string Hl7MessageId = "Hl7MessageId";

    /// <summary>HL7 message type (ADT, ORM, ORU, etc.).</summary>
    public const string Hl7MessageType = "Hl7MessageType";

    /// <summary>HL7 trigger event (A01, A04, O01, R01, etc.).</summary>
    public const string Hl7TriggerEvent = "Hl7TriggerEvent";

    /// <summary>HL7 sending facility name.</summary>
    public const string Hl7SendingFacility = "Hl7SendingFacility";

    /// <summary>HL7 sending application name.</summary>
    public const string Hl7SendingApplication = "Hl7SendingApplication";

    /// <summary>HL7 patient identifier extracted from PID segment.</summary>
    public const string Hl7PatientId = "Hl7PatientId";

    /// <summary>HL7 accession number extracted from OBR/ORC segment.</summary>
    public const string Hl7AccessionNumber = "Hl7AccessionNumber";

    // ==================== Dispatch ====================

    /// <summary>Target Edge Node ID for message dispatch.</summary>
    public const string DispatchTargetNodeId = "DispatchTargetNodeId";

    /// <summary>Current dispatch attempt number.</summary>
    public const string DispatchAttempt = "DispatchAttempt";

    /// <summary>Dispatch status (Queued, Dispatching, Delivered, Failed).</summary>
    public const string DispatchStatus = "DispatchStatus";

    /// <summary>Total messages in current dispatch batch.</summary>
    public const string DispatchBatchSize = "DispatchBatchSize";

    // ==================== Node Management ====================

    /// <summary>Managed Edge Node identifier in node-management operations.</summary>
    public const string ManagedNodeId = "ManagedNodeId";

    /// <summary>Managed Edge Node status (Online, Offline, Degraded).</summary>
    public const string ManagedNodeStatus = "ManagedNodeStatus";
}
