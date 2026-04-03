using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Diagnostics;

/// <summary>
/// Creates structured logging scopes pre-enriched with DICOM context properties.
/// Ensures all log events within the scope carry study/series/instance UIDs
/// and operation metadata without requiring manual property injection.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// using (_logger.BeginDicomScope(studyUid, "SendToPacs"))
/// {
///     _logger.LogInformation("Starting transfer...");
///     // All logs here automatically include StudyInstanceUID, OperationName, etc.
/// }
/// </code>
/// </remarks>
public static class DicomLogScope
{
    /// <summary>
    /// Begins a logging scope with DICOM study context and optional operation name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="studyInstanceUid">Study Instance UID.</param>
    /// <param name="operationName">Logical operation name.</param>
    /// <param name="callingAeTitle">Source AE Title (optional).</param>
    /// <param name="calledAeTitle">Destination AE Title (optional).</param>
    /// <returns>A disposable scope that removes the properties when disposed.</returns>
    public static IDisposable? BeginDicomScope(
        this ILogger logger,
        string studyInstanceUid,
        string? operationName = null,
        string? callingAeTitle = null,
        string? calledAeTitle = null)
    {
        var state = new Dictionary<string, object?>
        {
            [LoggingConstants.StudyInstanceUid] = studyInstanceUid,
        };

        if (!string.IsNullOrWhiteSpace(operationName))
            state[LoggingConstants.OperationName] = operationName;

        if (!string.IsNullOrWhiteSpace(callingAeTitle))
            state[LoggingConstants.CallingAeTitle] = callingAeTitle;

        if (!string.IsNullOrWhiteSpace(calledAeTitle))
            state[LoggingConstants.CalledAeTitle] = calledAeTitle;

        return logger.BeginScope(state);
    }

    /// <summary>
    /// Begins a logging scope for a DICOM instance-level operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="studyInstanceUid">Study Instance UID.</param>
    /// <param name="seriesInstanceUid">Series Instance UID.</param>
    /// <param name="sopInstanceUid">SOP Instance UID.</param>
    /// <param name="operationName">Logical operation name.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable? BeginDicomInstanceScope(
        this ILogger logger,
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        string? operationName = null)
    {
        var state = new Dictionary<string, object?>
        {
            [LoggingConstants.StudyInstanceUid] = studyInstanceUid,
            [LoggingConstants.SeriesInstanceUid] = seriesInstanceUid,
            [LoggingConstants.SopInstanceUid] = sopInstanceUid,
        };

        if (!string.IsNullOrWhiteSpace(operationName))
            state[LoggingConstants.OperationName] = operationName;

        return logger.BeginScope(state);
    }

    /// <summary>
    /// Begins a logging scope for a DICOM transfer/send operation.
    /// Combines correlation, study context, destination, and retry metadata.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="studyInstanceUid">Study Instance UID.</param>
    /// <param name="destination">Target PACS or endpoint.</param>
    /// <param name="retryAttempt">Current retry attempt number (0 = first attempt).</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable? BeginTransferScope(
        this ILogger logger,
        string studyInstanceUid,
        string destination,
        int retryAttempt = 0)
    {
        var state = new Dictionary<string, object?>
        {
            [LoggingConstants.StudyInstanceUid] = studyInstanceUid,
            [LoggingConstants.OperationName] = "TransferStudy",
            [LoggingConstants.Destination] = destination,
            [LoggingConstants.RetryAttempt] = retryAttempt,
        };

        return logger.BeginScope(state);
    }
}
