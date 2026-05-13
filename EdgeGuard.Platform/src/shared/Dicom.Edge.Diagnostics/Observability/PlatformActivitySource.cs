using System.Diagnostics;
using Dicom.Edge.Diagnostics.Constants;

namespace Dicom.Edge.Diagnostics.Observability;

/// <summary>
/// Provides a shared <see cref="ActivitySource"/> for creating distributed tracing spans.
/// Registered as a singleton per platform instance (Hub or Edge Node).
/// </summary>
public sealed class PlatformActivitySource : IDisposable
{
    /// <summary>The underlying activity source for OpenTelemetry tracing.</summary>
    public ActivitySource Source { get; }

    public PlatformActivitySource(string name, string version = "1.0.0")
    {
        Source = new ActivitySource(name, version);
    }

    /// <summary>
    /// Starts a new activity (span) for a named operation.
    /// Returns null if no listener is attached (no-op when tracing is disabled).
    /// </summary>
    public Activity? StartOperation(
        string operationName,
        ActivityKind kind = ActivityKind.Internal)
    {
        return Source.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Starts a new activity enriched with DICOM study context.
    /// </summary>
    public Activity? StartStudyOperation(
        string operationName,
        string studyInstanceUid,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = Source.StartActivity(operationName, kind);
        activity?.SetTag(DiagnosticsConstants.StudyInstanceUid, studyInstanceUid);
        return activity;
    }

    /// <inheritdoc/>
    public void Dispose() => Source.Dispose();
}
