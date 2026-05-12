namespace Dicom.Edge.Abstractions.Monitoring;

/// <summary>
/// Notifies the Hub that a study has been completed and processed on this node,
/// so it can be represented in the Hub SPA.
/// </summary>
public interface IStudyHubNotifier
{
    /// <summary>
    /// Sends study metadata to the Hub. Fire-and-forget safe — never throws.
    /// Returns true if the Hub acknowledged the notification.
    /// </summary>
    Task<bool> NotifyStudyCompletedAsync(
        string nodeId,
        string studyInstanceUid,
        string? patientId,
        string? patientName,
        string? accessionNumber,
        int instanceCount,
        long totalSizeBytes,
        CancellationToken ct = default);
}
