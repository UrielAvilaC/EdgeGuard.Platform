namespace Dicom.Edge.Abstractions.Events;

/// <summary>
/// Signals the <c>StudyCompletionWatcherService</c> to run an immediate completion check
/// instead of waiting for the next scheduled poll cycle.
/// Intended to be called on DICOM association release to reduce detection latency
/// while the polling mechanism remains as a safety net for multi-association transfers.
/// </summary>
public interface IStudyCompletionTrigger
{
    /// <summary>
    /// Requests an immediate completion scan. Fire-and-forget; never throws.
    /// Multiple concurrent calls collapse into a single scan (bounded channel, drop-on-full).
    /// </summary>
    void RequestImmediateCheck();
}
