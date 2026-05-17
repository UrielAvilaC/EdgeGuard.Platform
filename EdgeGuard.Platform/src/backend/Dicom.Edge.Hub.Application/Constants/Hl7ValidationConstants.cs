namespace Dicom.Edge.Hub.Application.Constants;

/// <summary>
/// Constants for HL7 message validation including supported message types,
/// required segment definitions, validation error/warning templates, and routing defaults.
/// </summary>
public static class Hl7ValidationConstants
{
    // ==================== Message Types ====================

    /// <summary>ADT (Admission, Discharge, Transfer) message type.</summary>
    public const string AdtMessageType = "ADT";

    /// <summary>ORM (Order) message type.</summary>
    public const string OrmMessageType = "ORM";

    /// <summary>ORU (Observation Result) message type.</summary>
    public const string OruMessageType = "ORU";

    /// <summary>Placeholder for messages whose type could not be extracted.</summary>
    public const string UnknownMessageType = "UNKNOWN";

    // ==================== ADT Trigger Events ====================

    /// <summary>ADT^A01 — Patient Admission (alta hospitalaria). Only accepted ADT admission event.</summary>
    public const string AdtAdmitTrigger = "A01";

    /// <summary>ADT^A40 — Merge Patient Records. Requires MRG segment.</summary>
    public const string AdtMergePatientTrigger = "A40";

    /// <summary>
    /// ADT trigger events accepted by this system.
    /// A01 = Admit, A40 = Merge Patient.
    /// All other ADT trigger events (A02 Transfer, A03 Discharge, A04 Pre-Admit, A08 Update, etc.) are rejected.
    /// </summary>
    public static readonly HashSet<string> AllowedAdtTriggerEvents =
        new(StringComparer.OrdinalIgnoreCase) { AdtAdmitTrigger, AdtMergePatientTrigger };

    // ==================== HL7 Segment Names ====================

    /// <summary>Message Header segment.</summary>
    public const string MshSegment = "MSH";

    /// <summary>Event Type segment.</summary>
    public const string EvnSegment = "EVN";

    /// <summary>Patient Identification segment.</summary>
    public const string PidSegment = "PID";

    /// <summary>Patient Visit segment.</summary>
    public const string Pv1Segment = "PV1";

    /// <summary>Common Order segment.</summary>
    public const string OrcSegment = "ORC";

    /// <summary>Observation Request segment.</summary>
    public const string ObrSegment = "OBR";

    /// <summary>Observation Result segment.</summary>
    public const string ObxSegment = "OBX";

    /// <summary>Merge Patient Information segment.</summary>
    public const string MrgSegment = "MRG";

    // ==================== OBX Value Types ====================

    /// <summary>OBX-2 "RP" — Reference Pointer. OBX-5 contains a URL or external reference.</summary>
    public const string ObxValueTypeReferencePointer = "RP";

    /// <summary>OBX-2 "ED" — Encapsulated Data. May contain base64 image data or a reference.</summary>
    public const string ObxValueTypeEncapsulatedData = "ED";

    /// <summary>OBX-2 "TX" — Text value type. Accepted when OBX-5 looks like a URL.</summary>
    public const string ObxValueTypeText = "TX";

    // ==================== Validation Error Messages ====================

    /// <summary>Error when MSH.9 (message type) cannot be extracted.</summary>
    public const string MshMessageTypeError = "MSH.9: Message type could not be extracted";

    /// <summary>Format template for unsupported message type errors. {0} = base type.</summary>
    public const string UnsupportedTypeTemplate =
        "Unsupported message type '{0}'. Supported: ADT (A01/A40), ORM, ORU";

    /// <summary>Format template for unsupported ADT trigger event. {0} = received trigger.</summary>
    public const string AdtTriggerNotAllowedTemplate =
        "ADT^{0} is not supported. Only ADT^A01 (Admission) and ADT^A40 (Merge Patient) are accepted";

    /// <summary>Error when ADT^A40 arrives without a MRG segment.</summary>
    public const string AdtMergeRequiresMrgError =
        "ADT^A40 requires a MRG segment with a prior patient ID (MRG.1)";

    /// <summary>Error when MRG.1 (prior patient ID) is absent in a merge message.</summary>
    public const string MrgPriorPatientIdMissingError =
        "MRG.1: Prior Patient ID is required for patient merge (ADT^A40)";

    /// <summary>Format template for missing required segment errors. {0} = segment, {1} = base type.</summary>
    public const string MissingSegmentTemplate =
        "Required segment '{0}' missing for {1} message";

    /// <summary>Error when PID.3 (Patient ID) is not present.</summary>
    public const string PidPatientIdError = "PID.3: Patient ID is required";

    // ==================== Validation Warning Messages ====================

    /// <summary>Warning when MSH.3 (Sending Application) is empty.</summary>
    public const string MshSendingAppWarning = "MSH.3: Sending Application is empty";

    /// <summary>Warning when MSH.4 (Sending Facility) is empty.</summary>
    public const string MshSendingFacilityWarning = "MSH.4: Sending Facility is empty";

    /// <summary>Format template for missing accession number warning. {0} = base type.</summary>
    public const string ObrAccessionWarningTemplate =
        "OBR.2: Accession Number is empty for {0} message";

    /// <summary>Warning when PID.5 (Patient Name) is empty.</summary>
    public const string PidPatientNameWarning = "PID.5: Patient Name is empty";

    /// <summary>Warning when ORM message carries a MRG segment but MRG.3 (prior accession) is absent.</summary>
    public const string OrmMrgNoAccessionWarning =
        "ORM with MRG segment: MRG.3 (prior accession number) is empty — study reassignment may be incomplete";

    /// <summary>Warning when ORU has no OBX segments with extractable image links.</summary>
    public const string OruNoImageLinksWarning =
        "ORU^R01: no OBX segments with image links (RP/ED/URL) were found";

    // ==================== Routing Defaults ====================

    /// <summary>Rule ID used for fallback routing when no rule matches.</summary>
    public const string FallbackRuleId = "FALLBACK";

    /// <summary>Default priority assigned to fallback-routed messages.</summary>
    public const int FallbackPriority = 10;

    /// <summary>Reason reported when no route can be determined (no active nodes).</summary>
    public const string NoActiveNodesReason = "No active nodes available";
}
