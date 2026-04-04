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

    // ==================== Validation Error Messages ====================

    /// <summary>Error when MSH.9 (message type) cannot be extracted.</summary>
    public const string MshMessageTypeError = "MSH.9: Message type could not be extracted";

    /// <summary>Format template for unsupported message type errors. {0} = base type.</summary>
    public const string UnsupportedTypeTemplate =
        "Unsupported message type '{0}'. Supported: ADT, ORM, ORU";

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
        "OBR.18: Accession Number is empty for {0} message";

    /// <summary>Warning when PID.5 (Patient Name) is empty.</summary>
    public const string PidPatientNameWarning = "PID.5: Patient Name is empty";

    // ==================== Routing Defaults ====================

    /// <summary>Rule ID used for fallback routing when no rule matches.</summary>
    public const string FallbackRuleId = "FALLBACK";

    /// <summary>Default priority assigned to fallback-routed messages.</summary>
    public const int FallbackPriority = 10;

    /// <summary>Reason reported when no route can be determined (no active nodes).</summary>
    public const string NoActiveNodesReason = "No active nodes available";
}
