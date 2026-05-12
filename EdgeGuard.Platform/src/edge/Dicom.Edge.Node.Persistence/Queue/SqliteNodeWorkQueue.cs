using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Abstractions.Queue;
using Dicom.Edge.Common.Errors;
using Dicom.Edge.Common.Results;
using Dicom.Edge.Node.Queue;
using Dicom.Edge.Node.Persistence.Diagnostics;

namespace Dicom.Edge.Node.Persistence.Queue;

/// <summary>
/// SQLite-backed implementation of <see cref="INodeWorkQueue"/>.
/// Adapts the low-level <see cref="IEdgeQueue{EdgeQueueItem}"/> into domain-specific
/// <see cref="NodeWorkItem"/> semantics used by the processing pipeline.
/// </summary>
public sealed class SqliteNodeWorkQueue(
    IEdgeQueue<EdgeQueueItem> edgeQueue,
    IMetricsCollector metrics,
    ILogger<SqliteNodeWorkQueue> logger) : INodeWorkQueue
{
    public async Task<Result> EnqueueAsync(NodeWorkItem item, CancellationToken ct = default)
    {
        var queueItem = new EdgeQueueItem
        {
            Id = item.Id,
            StudyInstanceUid = item.StudyInstanceUid,
            Status = TransferStatus.Pending,
            Priority = item.Priority,
            Destination = item.TargetPacsId ?? "hub",
            CreatedAt = item.CreatedAt,
            Metadata = JsonSerializer.Serialize(new NodeWorkItemMetadata
            {
                Type = item.Type,
                SourceAeTitle = item.SourceAeTitle
            })
        };

        var result = await edgeQueue.EnqueueAsync(queueItem, item.Priority, ct);
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Work item {ItemId} enqueued for study {StudyUid} (type={Type}, priority={Priority})",
                item.Id, item.StudyInstanceUid, item.Type, item.Priority);
        }

        return result;
    }

    public async Task<Result<NodeWorkItem?>> DequeueAsync(CancellationToken ct = default)
    {
        var result = await edgeQueue.DequeueAsync(ct);

        if (result.IsFailure)
            return Result<NodeWorkItem?>.Failure(result.Error!);

        var queueItem = result.Value;
        if (queueItem is null)
            return Result<NodeWorkItem?>.Success(null);

        var metadata = !string.IsNullOrEmpty(queueItem.Metadata)
            ? JsonSerializer.Deserialize<NodeWorkItemMetadata>(queueItem.Metadata)
            : null;

        var workItem = new NodeWorkItem
        {
            Id = queueItem.Id,
            StudyInstanceUid = queueItem.StudyInstanceUid,
            Type = metadata?.Type ?? NodeWorkItemType.PacsSend,
            Priority = queueItem.Priority,
            SourceAeTitle = metadata?.SourceAeTitle,
            TargetPacsId = queueItem.Destination,
            CreatedAt = queueItem.CreatedAt,
            RetryCount = queueItem.RetryCount,
            LastError = queueItem.ErrorMessage
        };

        logger.LogDebug(
            "Dequeued work item {ItemId} for study {StudyUid} (type={Type}, retry={Retry})",
            workItem.Id, workItem.StudyInstanceUid, workItem.Type, workItem.RetryCount);

        return Result<NodeWorkItem?>.Success(workItem);
    }

    public async Task<Result<int>> GetPendingCountAsync(CancellationToken ct = default)
    {
        return await edgeQueue.GetCountAsync(ct);
    }

    public async Task<Result<int>> GetFailedCountAsync(CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await GetContextFactory().CreateDbContextAsync(ct);
            var count = await ctx.QueueItems
                .CountAsync(q => q.Status == TransferStatus.Failed, ct);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get failed item count");
            return Result<int>.Failure(new Error("QUEUE_FAILED_COUNT_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Resolves the factory from the same DI container — workaround for direct
    /// DB access needed by GetFailedCountAsync (not exposed by IEdgeQueue).
    /// </summary>
    private IDbContextFactory<EdgeNodeDbContext> GetContextFactory()
    {
        // The edge queue already uses the factory internally; we need a second
        // path for the failed-count query that IEdgeQueue does not expose.
        // This is injected via the overloaded constructor below.
        return _factory;
    }

    private readonly IDbContextFactory<EdgeNodeDbContext> _factory = null!;

    /// <summary>
    /// Primary constructor with full dependency set.
    /// </summary>
    public SqliteNodeWorkQueue(
        IEdgeQueue<EdgeQueueItem> edgeQueue,
        IDbContextFactory<EdgeNodeDbContext> factory,
        IMetricsCollector metrics,
        ILogger<SqliteNodeWorkQueue> logger) : this(edgeQueue, metrics, logger)
    {
        _factory = factory;
    }
}

/// <summary>
/// Metadata serialized into <see cref="EdgeQueueItem.Metadata"/> for round-tripping
/// domain-specific fields that <see cref="EdgeQueueItem"/> does not natively carry.
/// </summary>
internal sealed class NodeWorkItemMetadata
{
    public NodeWorkItemType Type { get; set; }
    public string? SourceAeTitle { get; set; }
}
