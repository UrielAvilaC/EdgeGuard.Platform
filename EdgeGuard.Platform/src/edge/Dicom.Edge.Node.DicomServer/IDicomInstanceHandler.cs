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
    Task HandleInstanceAsync(
        DicomDataset dataset,
        string callingAeTitle,
        CancellationToken ct = default);
}
