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
            SizeBytes = item.TotalSizeBytes,
            InstanceCount = item.InstanceCount,
            Metadata = JsonSerializer.Serialize(new NodeWorkItemMetadata
            {
                Type = item.Type,
                SourceAeTitle = item.SourceAeTitle,
                PatientId = item.PatientId,
                PatientName = item.PatientName,
                AccessionNumber = item.AccessionNumber,
                ExplicitPacsIds = item.ExplicitPacsIds
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
            LastError = queueItem.ErrorMessage,
            PatientId = metadata?.PatientId,
            PatientName = metadata?.PatientName,
            AccessionNumber = metadata?.AccessionNumber,
            TotalSizeBytes = queueItem.SizeBytes,
            InstanceCount = queueItem.InstanceCount,
            ExplicitPacsIds = metadata?.ExplicitPacsIds
        };

        logger.LogDebug(
            "Dequeued work item {ItemId} for study {StudyUid} (type={Type}, retry={Retry})",
            workItem.Id, workItem.StudyInstanceUid, workItem.Type, workItem.RetryCount);

        return Result<NodeWorkItem?>.Success(workItem);
    }

    public async Task<Result> CompleteAsync(NodeWorkItem item, CancellationToken ct = default)
    {
        var result = await edgeQueue.RemoveAsync(
            new EdgeQueueItem { Id = item.Id, StudyInstanceUid = item.StudyInstanceUid }, ct);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Work item {ItemId} completed and removed from queue (study {StudyUid})",
                item.Id, item.StudyInstanceUid);
        }

        return result;
    }

    public async Task<Result> FailAsync(NodeWorkItem item, string? error, CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await GetContextFactory().CreateDbContextAsync(ct);
            var entity = await ctx.QueueItems.FindAsync([item.Id], ct);

            if (entity is null)
                return Result.Failure(new Error("QUEUE_ITEM_NOT_FOUND", $"Queue item {item.Id} not found"));

            entity.Status = TransferStatus.Failed;
            entity.ErrorMessage = error;
            entity.IsLocked = false;
            entity.LockedAt = null;
            entity.LockedBy = null;
            await ctx.SaveChangesAsync(ct);

            logger.LogWarning(
                "Work item {ItemId} marked Failed (study {StudyUid}): {Error}",
                item.Id, item.StudyInstanceUid, error);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark work item {ItemId} as failed", item.Id);
            return Result.Failure(new Error("QUEUE_FAIL_ERROR", ex.Message));
        }
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
    /// Exponential backoff schedule applied on requeue, indexed by the item's
    /// (pre-increment) <c>RetryCount</c>. Matches the policy documented on
    /// <see cref="EdgeQueueItem.NextRetryAt"/> (1min, 5min, 15min, 1hr, ...), capped at
    /// 10 minutes so a reconnected PACS drains its backlog within a reasonable time.
    /// </summary>
    private static readonly TimeSpan[] BackoffSchedule =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromMinutes(60),
    ];

    public async Task<Result<int>> RequeueFailedAsync(string? destination, CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await GetContextFactory().CreateDbContextAsync(ct);

            var candidates = await ctx.QueueItems
                .Where(q => q.Status == TransferStatus.Failed && q.RetryCount < 10)
                .Where(q => destination == null || q.Destination == destination || q.Destination == "hub")
                .ToListAsync(ct);

            if (candidates.Count == 0)
                return Result<int>.Success(0);

            var now = DateTime.UtcNow;
            foreach (var entity in candidates)
            {
                var backoff = BackoffSchedule[Math.Min(entity.RetryCount, BackoffSchedule.Length - 1)];

                entity.Status = TransferStatus.Pending;
                entity.RetryCount++;
                entity.NextRetryAt = now.Add(backoff);
                entity.IsLocked = false;
                entity.LockedAt = null;
                entity.LockedBy = null;
            }

            await ctx.SaveChangesAsync(ct);

            logger.LogInformation(
                "Requeued {Count} failed item(s) for retry (destination={Destination})",
                candidates.Count, destination ?? "<any>");

            return Result<int>.Success(candidates.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to requeue failed items (destination={Destination})", destination);
            return Result<int>.Failure(new Error("QUEUE_REQUEUE_FAILED_ERROR", ex.Message));
        }
    }

    public async Task<Result> RequeueStudyAsync(
        string studyInstanceUid, IReadOnlyList<string> pacsIds, CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await GetContextFactory().CreateDbContextAsync(ct);

            var entity = await ctx.QueueItems
                .Where(q => q.StudyInstanceUid == studyInstanceUid)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (entity is null)
                return Result.Failure(new Error("QUEUE_ITEM_NOT_FOUND",
                    $"No queue item found for study {studyInstanceUid}"));

            var metadata = !string.IsNullOrEmpty(entity.Metadata)
                ? JsonSerializer.Deserialize<NodeWorkItemMetadata>(entity.Metadata)
                : new NodeWorkItemMetadata();
            metadata!.ExplicitPacsIds = pacsIds;
            entity.Metadata = JsonSerializer.Serialize(metadata);

            entity.Status = TransferStatus.Pending;
            entity.NextRetryAt = null;
            entity.IsLocked = false;
            entity.LockedAt = null;
            entity.LockedBy = null;
            entity.ErrorMessage = null;

            await ctx.SaveChangesAsync(ct);

            logger.LogInformation(
                "Manually requeued study {StudyUid} for {Count} explicit PACS target(s): [{PacsIds}]",
                studyInstanceUid, pacsIds.Count, string.Join(", ", pacsIds));

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to manually requeue study {StudyUid}", studyInstanceUid);
            return Result.Failure(new Error("QUEUE_REQUEUE_STUDY_ERROR", ex.Message));
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
        ILogger<SqliteNodeWorkQueue> logger) : this(edgeQueue, logger)
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
    public string? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? AccessionNumber { get; set; }
    public IReadOnlyList<string>? ExplicitPacsIds { get; set; }
}
