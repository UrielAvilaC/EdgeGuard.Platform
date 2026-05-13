namespace Dicom.Edge.Hub.Infrastructure.Constants;

/// <summary>
/// Constants for the message dispatch infrastructure including HTTP client
/// configuration, error messages, and timeout defaults.
/// </summary>
public static class DispatchConstants
{
    // ==================== HTTP Client ====================

    /// <summary>Named HTTP client identifier used for node dispatch requests.</summary>
    public const string HttpClientName = "NodeDispatch";

    /// <summary>Default timeout in seconds for dispatch HTTP requests.</summary>
    public const int DefaultTimeoutSeconds = 30;

    // ==================== Error Messages ====================

    /// <summary>Error message when the target node cannot be found or has no API endpoint.</summary>
    public const string TargetNodeNotFoundMessage =
        "Target node not found or has no API endpoint.";

    /// <summary>Error message when a node rejects the dispatched message without providing a reason.</summary>
    public const string NodeRejectedMessage =
        "Node rejected the message without reason.";

    /// <summary>Error message when a dispatch operation exceeds the timeout.</summary>
    public const string DispatchTimeoutMessage = "Dispatch timed out.";

    /// <summary>Format template for connection error messages. {0} = exception message.</summary>
    public const string ConnectionErrorTemplate = "Connection error: {0}";

    /// <summary>Format template for HTTP error responses. {0} = status code, {1} = body.</summary>
    public const string HttpErrorTemplate = "HTTP {0}: {1}";

    /// <summary>Format template when max dispatch retries are exceeded. {0} = last error.</summary>
    public const string MaxRetriesExceededTemplate = "Max retries exceeded. Last error: {0}";
}
