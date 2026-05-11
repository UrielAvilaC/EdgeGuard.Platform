using Dicom.Edge.Diagnostics.Constants;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Diagnostics.Scopes;

/// <summary>
/// Creates structured logging scopes pre-enriched with DICOM context properties.
/// Ensures all log events within the scope carry study/series/instance UIDs
/// and operation metadata without requiring manual property injection.
/// </summary>
public static class DicomLogScope
{
    /// <summary>
    /// Begins a logging scope with DICOM study context and optional operation name.
    /// </summary>
    public static IDisposable? BeginDicomScope(
        this ILogger logger,
        string studyInstanceUid,
        string? operationName = null,
        string? callingAeTitle = null,
        string? calledAeTitle = null)
    {
        var state = new Dictionary<string, object?>
        {
            [DiagnosticsConstants.StudyInstanceUid] = studyInstanceUid,
        };

        if (!string.IsNullOrWhiteSpace(operationName))
            state[DiagnosticsConstants.OperationName] = operationName;

        if (!string.IsNullOrWhiteSpace(callingAeTitle))
            state[DiagnosticsConstants.CallingAeTitle] = callingAeTitle;

        if (!string.IsNullOrWhiteSpace(calledAeTitle))
            state[DiagnosticsConstants.CalledAeTitle] = calledAeTitle;

        return logger.BeginScope(state);
    }

    /// <summary>
    /// Begins a logging scope for a DICOM instance-level operation.
    /// </summary>
    public static IDisposable? BeginDicomInstanceScope(
        this ILogger logger,
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        string? operationName = null)
    {
        var state = new Dictionary<string, object?>
        {
            [DiagnosticsConstants.StudyInstanceUid] = studyInstanceUid,
            [DiagnosticsConstants.SeriesInstanceUid] = seriesInstanceUid,
            [DiagnosticsConstants.SopInstanceUid] = sopInstanceUid,
        };

        if (!string.IsNullOrWhiteSpace(operationName))
            state[DiagnosticsConstants.OperationName] = operationName;

        return logger.BeginScope(state);
    }

    /// <summary>
    /// Begins a logging scope for a DICOM transfer/send operation.
    /// </summary>
    public static IDisposable? BeginTransferScope(
        this ILogger logger,
        string studyInstanceUid,
        string destination,
        int retryAttempt = 0)
    {
        var state = new Dictionary<string, object?>
        {
            [DiagnosticsConstants.StudyInstanceUid] = studyInstanceUid,
            [DiagnosticsConstants.OperationName] = "TransferStudy",
            [DiagnosticsConstants.Destination] = destination,
            [DiagnosticsConstants.RetryAttempt] = retryAttempt,
        };

        return logger.BeginScope(state);
    }
}
