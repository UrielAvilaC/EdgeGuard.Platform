namespace Dicom.Edge.Node.Diagnostics;

/// <summary>
/// Standardized property names used across all log events for consistency and queryability.
/// </summary>
public static class LoggingConstants
{
    // ==================== Identity ====================

    /// <summary>Application name property.</summary>
    public const string Application = "Application";

    /// <summary>Logical component property (DicomServer, Sender, Queue, etc.).</summary>
    public const string Component = "Component";

    /// <summary>Unique Edge Node identifier property.</summary>
    public const string NodeId = "NodeId";

    /// <summary>Deployment environment property.</summary>
    public const string Environment = "Environment";

    // ==================== Correlation ====================

    /// <summary>Correlation ID for request/event tracing.</summary>
    public const string CorrelationId = "CorrelationId";

    /// <summary>Causation ID linking to the parent event.</summary>
    public const string CausationId = "CausationId";

    // ==================== DICOM Context ====================

    /// <summary>Study Instance UID.</summary>
    public const string StudyInstanceUid = "StudyInstanceUID";

    /// <summary>Series Instance UID.</summary>
    public const string SeriesInstanceUid = "SeriesInstanceUID";

    /// <summary>SOP Instance UID.</summary>
    public const string SopInstanceUid = "SOPInstanceUID";

    /// <summary>DICOM AE Title of the calling entity.</summary>
    public const string CallingAeTitle = "CallingAeTitle";

    /// <summary>DICOM AE Title of the called entity.</summary>
    public const string CalledAeTitle = "CalledAeTitle";

    /// <summary>DICOM modality type (CT, MR, CR, US, etc.).</summary>
    public const string Modality = "Modality";

    // ==================== Operations ====================

    /// <summary>Operation name being performed.</summary>
    public const string OperationName = "OperationName";

    /// <summary>Duration of the operation in milliseconds.</summary>
    public const string DurationMs = "DurationMs";

    /// <summary>Destination endpoint or AE Title for send operations.</summary>
    public const string Destination = "Destination";

    /// <summary>Whether the operation completed successfully.</summary>
    public const string Success = "Success";

    /// <summary>Retry attempt number.</summary>
    public const string RetryAttempt = "RetryAttempt";

    // ==================== Metrics ====================

    /// <summary>Meter name for OpenTelemetry instrumentation.</summary>
    public const string MeterName = "Dicom.Edge.Node";

    /// <summary>Activity source name for distributed tracing.</summary>
    public const string ActivitySourceName = "Dicom.Edge.Node";
}
