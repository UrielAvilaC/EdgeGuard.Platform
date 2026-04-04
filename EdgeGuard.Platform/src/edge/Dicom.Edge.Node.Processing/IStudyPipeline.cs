namespace Dicom.Edge.Node.Processing;

/// <summary>
/// Orchestrates the full study processing pipeline:
/// study completion detection → routing → PACS send → Hub notification.
/// </summary>
public interface IStudyPipeline
{
    /// <summary>
    /// Processes a completed study through the full pipeline.
    /// </summary>
    Task<StudyPipelineResult> ProcessStudyAsync(string studyInstanceUid, CancellationToken ct = default);
}
