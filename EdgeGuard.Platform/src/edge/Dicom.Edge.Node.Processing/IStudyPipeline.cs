using Dicom.Edge.Node.Queue;

namespace Dicom.Edge.Node.Processing;

/// <summary>
/// Orchestrates the full study processing pipeline:
/// study completion detection → routing → parallel PACS send → Hub notification.
/// </summary>
public interface IStudyPipeline
{
    /// <summary>
    /// Processes a completed study through the full pipeline.
    /// The <paramref name="workItem"/> carries study metadata so the Hub
    /// can be notified with full patient context.
    /// </summary>
    Task<StudyPipelineResult> ProcessStudyAsync(NodeWorkItem workItem, CancellationToken ct = default);
}
