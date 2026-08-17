using FellowOakDicom;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Callback interface for DICOM instances received by the C-STORE SCP.
/// Implementations handle storage, metadata extraction, and queue enqueue.
/// </summary>
public interface IDicomInstanceHandler
{
    /// <summary>
    /// Called for each DICOM instance (image) received via C-STORE.
    /// </summary>
    /// <returns>
    /// Bytes written to local storage, or <c>0</c> when the instance was skipped.
    /// Reported by the SCP in the per-association log.
    /// </returns>
    Task<long> HandleInstanceAsync(
        DicomDataset dataset,
        string callingAeTitle,
        CancellationToken ct = default);
}
