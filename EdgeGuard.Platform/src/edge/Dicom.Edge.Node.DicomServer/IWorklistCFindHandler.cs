using FellowOakDicom;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Callback interface for DICOM Modality Worklist (MWL) C-FIND queries received by the SCP.
/// Implementations match query keys against the local worklist and return DICOM datasets.
/// </summary>
public interface IWorklistCFindHandler
{
    /// <summary>
    /// Queries the local worklist using the supplied DICOM C-FIND keys
    /// and yields matching response datasets.
    /// </summary>
    IAsyncEnumerable<DicomDataset> QueryWorklistAsync(
        DicomDataset queryKeys,
        CancellationToken ct = default);
}
