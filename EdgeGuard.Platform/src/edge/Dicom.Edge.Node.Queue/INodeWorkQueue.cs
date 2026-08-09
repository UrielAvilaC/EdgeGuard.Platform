using Dicom.Edge.Common.Results;

namespace Dicom.Edge.Node.Queue;

/// <summary>
/// High-level queue operations for the node processing pipeline.
/// Wraps <see cref="Dicom.Edge.Abstractions.Queue.IEdgeQueue{T}"/> with domain-specific semantics.
/// </summary>
public interface INodeWorkQueue
{
    Task<Result> EnqueueAsync(NodeWorkItem item, CancellationToken ct = default);
    Task<Result<NodeWorkItem?>> DequeueAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks a work item as successfully processed and removes it from the queue.
    /// Called after the pipeline completes so finished studies do not linger in the
    /// queue as locked <c>Sending</c> rows forever.
    /// </summary>
    Task<Result> CompleteAsync(NodeWorkItem item, CancellationToken ct = default);

    /// <summary>
    /// Marks a work item as failed (status <c>Failed</c>, lock released) without removing it,
    /// so it stays available for diagnostics / manual retry.
    /// </summary>
    Task<Result> FailAsync(NodeWorkItem item, string? error, CancellationToken ct = default);

    Task<Result<int>> GetPendingCountAsync(CancellationToken ct = default);
    Task<Result<int>> GetFailedCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Re-queues <c>Failed</c> items that are eligible for retry (under the max retry
    /// count) so the processing pipeline picks them up again, applying exponential
    /// backoff based on each item's <c>RetryCount</c>.
    /// </summary>
    /// <param name="destination">
    /// When provided, only items sent to this destination (matched against
    /// <see cref="NodeWorkItem.TargetPacsId"/>, or the legacy <c>"hub"</c> placeholder used
    /// when no specific destination was recorded) are requeued. When null, all eligible
    /// failed items are requeued regardless of destination.
    /// </param>
    /// <returns>The number of items requeued.</returns>
    Task<Result<int>> RequeueFailedAsync(string? destination, CancellationToken ct = default);

    /// <summary>
    /// Manual resend: re-queues the most recent work item for <paramref name="studyInstanceUid"/>
    /// (regardless of its current status) so the pipeline sends it again to exactly the given
    /// PACS (matched by <c>NodePacsServer.Id</c>), bypassing routing rules. Unlike
    /// <see cref="RequeueFailedAsync"/>, this is an explicit user action and does not respect
    /// the max-retry cap.
    /// </summary>
    Task<Result> RequeueStudyAsync(
        string studyInstanceUid, IReadOnlyList<string> pacsIds, CancellationToken ct = default);
}
