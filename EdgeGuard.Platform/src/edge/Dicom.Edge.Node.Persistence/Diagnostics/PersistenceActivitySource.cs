using System.Diagnostics;

namespace Dicom.Edge.Node.Persistence.Diagnostics;

/// <summary>
/// Dedicated <see cref="ActivitySource"/> for Persistence-layer tracing.
/// Creates spans for all critical DB operations (seed, cleanup, study completion, settings).
/// Automatically feeds into OpenTelemetry when a listener is registered for this source name.
/// </summary>
/// <remarks>
/// Register with OTEL via <c>.AddSource(PersistenceActivitySource.SourceName)</c>.
/// When no listener is attached all methods return null (zero-cost no-op).
/// </remarks>
internal static class PersistenceActivitySource
{
    public const string SourceName = "Dicom.Edge.Node.Persistence";

    private static readonly ActivitySource Source = new(SourceName, "1.0.0");

    // ── Span factories ────────────────────────────────────────────────────────

    public static Activity? StartSeedCheck()
        => Source.StartActivity("Persistence.SeedCheck", ActivityKind.Internal);

    public static Activity? StartSettingsReload()
        => Source.StartActivity("Persistence.SettingsReload", ActivityKind.Internal);

    public static Activity? StartSettingsSet(string key)
    {
        var activity = Source.StartActivity("Persistence.SettingsSet", ActivityKind.Internal);
        activity?.SetTag("persistence.setting.key", key);
        return activity;
    }

    public static Activity? StartSettingsBatch(int count)
    {
        var activity = Source.StartActivity("Persistence.SettingsBatch", ActivityKind.Internal);
        activity?.SetTag("persistence.batch.count", count);
        return activity;
    }

    public static Activity? StartCompletionScan()
        => Source.StartActivity("Persistence.StudyCompletionScan", ActivityKind.Internal);

    public static Activity? StartStudyComplete(string studyUid)
    {
        var activity = Source.StartActivity("Persistence.StudyComplete", ActivityKind.Internal);
        activity?.SetTag("dicom.study.uid", studyUid);
        return activity;
    }

    public static Activity? StartCleanupCycle()
        => Source.StartActivity("Persistence.CleanupCycle", ActivityKind.Internal);

    public static Activity? StartCleanupPhase(string phaseName)
    {
        var activity = Source.StartActivity($"Persistence.Cleanup.{phaseName}", ActivityKind.Internal);
        activity?.SetTag("persistence.cleanup.phase", phaseName);
        return activity;
    }

    public static Activity? StartPurgeFiles(string studyUid)
    {
        var activity = Source.StartActivity("Persistence.PurgeFiles", ActivityKind.Internal);
        activity?.SetTag("dicom.study.uid", studyUid);
        return activity;
    }

    // ── Queue spans ───────────────────────────────────────────────────────────

    public static Activity? StartQueueEnqueue(string studyUid)
    {
        var activity = Source.StartActivity("Persistence.Queue.Enqueue", ActivityKind.Producer);
        activity?.SetTag("dicom.study.uid", studyUid);
        return activity;
    }

    public static Activity? StartQueueBatch(int count)
    {
        var activity = Source.StartActivity("Persistence.Queue.EnqueueBatch", ActivityKind.Producer);
        activity?.SetTag("queue.batch.count", count);
        return activity;
    }

    public static Activity? StartQueueDequeue()
        => Source.StartActivity("Persistence.Queue.Dequeue", ActivityKind.Consumer);

    public static Activity? StartQueueRequeue(string studyUid)
    {
        var activity = Source.StartActivity("Persistence.Queue.Requeue", ActivityKind.Internal);
        activity?.SetTag("dicom.study.uid", studyUid);
        return activity;
    }
}
