namespace Dicom.Edge.Diagnostics.Constants;

/// <summary>
/// Constants used by middleware components including exception handling
/// and correlation ID propagation.
/// </summary>
public static class MiddlewareConstants
{
    /// <summary>Standard error message returned to clients for unhandled exceptions.</summary>
    public const string InternalErrorMessage = "An internal error occurred.";

    /// <summary>Fallback value when a correlation ID cannot be resolved.</summary>
    public const string UnknownCorrelationId = "N/A";

    /// <summary>HTTP status code for client-closed requests (non-standard).</summary>
    public const int ClientClosedRequestStatusCode = 499;
}
