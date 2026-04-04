namespace Dicom.Edge.Node.Sender;

/// <summary>
/// Interface for sending DICOM studies to PACS via C-STORE SCU.
/// </summary>
public interface IPacsSender
{
    /// <summary>
    /// Sends all DICOM files for a study to a destination PACS.
    /// </summary>
    Task<PacsSendResult> SendStudyAsync(
        string studyInstanceUid,
        PacsDestination destination,
        CancellationToken ct = default);

    /// <summary>
    /// Performs a C-ECHO verification against a PACS.
    /// </summary>
    Task<bool> VerifyConnectionAsync(PacsDestination destination, CancellationToken ct = default);
}

/// <summary>
/// Result of a PACS send operation.
/// </summary>
public sealed class PacsSendResult
{
    public required bool Success { get; init; }
    public required string StudyInstanceUid { get; init; }
    public required string DestinationAeTitle { get; init; }
    public int InstancesSent { get; init; }
    public int InstancesFailed { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Error { get; init; }

    public static PacsSendResult Ok(string studyUid, string destAe, int sent, TimeSpan duration) =>
        new()
        {
            Success = true,
            StudyInstanceUid = studyUid,
            DestinationAeTitle = destAe,
            InstancesSent = sent,
            Duration = duration
        };

    public static PacsSendResult Fail(string studyUid, string destAe, string error) =>
        new()
        {
            Success = false,
            StudyInstanceUid = studyUid,
            DestinationAeTitle = destAe,
            Error = error
        };
}
