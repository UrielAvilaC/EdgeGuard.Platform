using FellowOakDicom;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Callback interface for DICOM Modality Worklist (MWL) C-FIND queries received by the SCP.
/// Implementations match query keys against the local worklist and return DICOM datasets.
/// </summary>
public interface IWorklistCFindHandler
{
    /// <summary>
    /// Queries the local worklist using the supplied DICOM C-FIND keys and yields matching
    /// response datasets, constrained to what the calling equipment is allowed to see
    /// (its assigned modalities and optional scheduled station AE).
    /// </summary>
    /// <param name="callingAe">The SCU calling AE title; resolves the equipment catalog entry.</param>
    IAsyncEnumerable<DicomDataset> QueryWorklistAsync(
        DicomDataset queryKeys,
        string callingAe,
        CancellationToken ct = default);
}
