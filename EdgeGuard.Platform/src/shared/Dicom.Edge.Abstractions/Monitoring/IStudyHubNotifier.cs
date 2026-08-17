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
        DateTime? studyDate = null,
        string? studyDescription = null,
        int seriesCount = 0,
        DateTime? patientBirthDate = null,
        string? patientSex = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends incremental progress (per-instance) to the Hub while a study is being received.
    /// Throttle strategy is left to the caller. Fire-and-forget safe — never throws.
    /// </summary>
    Task<bool> NotifyStudyProgressAsync(
        string nodeId,
        string studyInstanceUid,
        string? accessionNumber,
        string? patientId,
        string? patientName,
        int instanceCount,
        long totalSizeBytes,
        DateTime? studyDate = null,
        string? studyDescription = null,
        int seriesCount = 0,
        DateTime? patientBirthDate = null,
        string? patientSex = null,
        CancellationToken ct = default);

    /// <summary>
    /// Reports the PACS-send phase of a study so the Hub advances its status to
    /// "Enviando a PACS" / "Enviado a PACS" / "Failed". Fire-and-forget safe — never throws.
    /// </summary>
    /// <param name="status">One of <c>Sending</c>, <c>SentToPacs</c> or <c>Failed</c> (matches <c>StudyStatus</c>).</param>
    Task<bool> NotifyPacsSendStatusAsync(
        string nodeId,
        string studyInstanceUid,
        string status,
        string? targetPacsAeTitle = null,
        string? error = null,
        CancellationToken ct = default);
}
