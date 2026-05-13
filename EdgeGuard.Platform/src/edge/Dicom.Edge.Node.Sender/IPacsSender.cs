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
    /// Returns a rich result with success status, latency, and structured error reason.
    /// </summary>
    Task<PacsCEchoVerifyResult> VerifyConnectionAsync(PacsDestination destination, CancellationToken ct = default);
}

/// <summary>
/// Rich result of a C-ECHO verification attempt, including the DICOM rejection reason
/// when the association was refused by the remote AE.
/// </summary>
/// <param name="Success">True if C-ECHO responded with DICOM Success status.</param>
/// <param name="ErrorMessage">Exception or DICOM failure message. Null when successful.</param>
/// <param name="ErrorReason">
/// Structured rejection reason extracted from <c>DicomAssociationRejectedException</c>,
/// e.g. "CalledAENotRecognized". Null when successful or error is non-DICOM.
/// </param>
/// <param name="LatencyMs">Round-trip time in milliseconds. Null if the attempt failed before completion.</param>
public sealed record PacsCEchoVerifyResult(
    bool Success,
    string? ErrorMessage = null,
    string? ErrorReason = null,
    double? LatencyMs = null)
{
    public static PacsCEchoVerifyResult Ok(double latencyMs) =>
        new(true, LatencyMs: latencyMs);

    public static PacsCEchoVerifyResult Fail(string message, string? reason = null) =>
        new(false, message, reason);
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
