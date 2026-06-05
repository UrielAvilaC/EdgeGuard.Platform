namespace Dicom.Edge.Hub.Infrastructure.Constants;

/// <summary>
/// Constants for the HL7 v2.x protocol including MLLP framing delimiters,
/// ACK construction values, sender identifiers, and version descriptors.
/// </summary>
public static class Hl7ProtocolConstants
{
    // ==================== MLLP Framing Delimiters ====================

    /// <summary>MLLP start-of-block character (VT / 0x0B).</summary>
    public const char StartBlock = '\x0B';

    /// <summary>MLLP end-of-block character (FS / 0x1C).</summary>
    public const char EndBlock = '\x1C';

    /// <summary>HL7 segment terminator (CR / 0x0D).</summary>
    public const char SegmentTerminator = '\r';

    // ==================== ACK Construction ====================

    /// <summary>Acknowledgment code for a successfully accepted message.</summary>
    public const string AckCode = "AA";

    /// <summary>NACK: Application Error — message accepted but processing failed (P0-5).</summary>
    public const string NackErrorCode = "AE";

    /// <summary>NACK: Application Reject — message rejected at the protocol level (P0-5).</summary>
    public const string NackRejectCode = "AR";

    /// <summary>HL7 message type identifier for acknowledgment messages.</summary>
    public const string AckMessageType = "ACK";

    /// <summary>HL7 processing mode identifier (P = Production).</summary>
    public const string ProcessingId = "P";

    /// <summary>HL7 version number used in ACK headers.</summary>
    public const string Hl7Version = "2.5";

    // ==================== Sender Identity ====================

    /// <summary>Sending application name used in ACK MSH.3.</summary>
    public const string SenderApplication = "EdgeGuardHub";

    /// <summary>Sending facility name used in ACK MSH.4.</summary>
    public const string SenderFacility = "EdgeGuard";

    // ==================== Endpoint Defaults ====================

    /// <summary>Default endpoint identifier when the remote endpoint cannot be resolved.</summary>
    public const string UnknownEndpoint = "unknown";
}
