using System.Diagnostics;

namespace Dicom.Edge.Node.Diagnostics.Observability;

/// <summary>
/// Provides a shared <see cref="ActivitySource"/> for creating distributed tracing spans
/// across all Edge Node components. Use this to instrument critical operations
/// (study receive, queue processing, PACS send) with spans.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// using var activity = EdgeActivitySource.Instance.StartActivity("ProcessStudy");
/// activity?.SetTag(LoggingConstants.StudyInstanceUid, studyUid);
/// // ... do work ...
/// activity?.SetStatus(ActivityStatusCode.Ok);
/// </code>
/// </remarks>
public static class EdgeActivitySource
{
    /// <summary>
    /// Shared activity source for all Edge Node tracing.
    /// Registered with OpenTelemetry via <see cref="LoggingConstants.ActivitySourceName"/>.
    /// </summary>
    public static readonly ActivitySource Instance = new(
        LoggingConstants.ActivitySourceName,
        "1.0.0");

    /// <summary>
    /// Starts a new activity (span) for a named operation.
    /// Returns null if no listener is attached (no-op when tracing is disabled).
    /// </summary>
    /// <param name="operationName">Name of the operation (e.g., "ReceiveStudy", "SendToPacs").</param>
    /// <param name="kind">The span kind (default: Internal).</param>
    /// <returns>The started activity, or null.</returns>
    public static Activity? StartOperation(
        string operationName,
        ActivityKind kind = ActivityKind.Internal)
    {
        return Instance.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Starts a new activity enriched with DICOM study context.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="studyInstanceUid">Study Instance UID to tag on the span.</param>
    /// <param name="kind">The span kind.</param>
    /// <returns>The started activity, or null.</returns>
    public static Activity? StartStudyOperation(
        string operationName,
        string studyInstanceUid,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = Instance.StartActivity(operationName, kind);
        activity?.SetTag(LoggingConstants.StudyInstanceUid, studyInstanceUid);
        return activity;
    }
}
