using FellowOakDicom;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Callback interface for DICOM Study Root Query/Retrieve C-FIND queries received by the SCP.
/// Implementations match incoming query keys against local studies and return response datasets.
/// </summary>
public interface IStudyRootCFindHandler
{
    /// <summary>
    /// Queries local studies using the supplied C-FIND query keys (Study Root model)
    /// and yields matching response datasets.
    /// </summary>
    IAsyncEnumerable<DicomDataset> QueryStudiesAsync(
        DicomDataset queryKeys,
        CancellationToken ct = default);
}
