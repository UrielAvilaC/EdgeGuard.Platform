using System.Diagnostics;
using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Abstractions.Queue;
using Dicom.Edge.Common.Errors;
using Dicom.Edge.Common.Results;
using Dicom.Edge.Node.Persistence.Diagnostics;

namespace Dicom.Edge.Node.Persistence.Queue;

/// <summary>
/// SQLite-backed implementation of <see cref="IEdgeQueue{T}"/> for <see cref="EdgeQueueItem"/>.
/// Uses <see cref="IDbContextFactory{TContext}"/> to remain Singleton-safe.
/// All queue operations are atomic at the DB level; the dequeue uses optimistic locking
/// via <c>IsLocked</c> + <c>LockedBy</c> to prevent duplicate processing.
/// </summary>
public sealed class SqliteEdgeQueue(
    IDbContextFactory<EdgeNodeDbContext> factory,
    IMetricsCollector metrics,
    ILogger<SqliteEdgeQueue> logger) : IEdgeQueue<EdgeQueueItem>
{
    private static readonly string WorkerId = $"{Environment.MachineName}-{Environment.ProcessId}";

    private static readonly TimeSpan StaleLockThreshold = TimeSpan.FromMinutes(10);

    // ── Enqueue ───────────────────────────────────────────────────────────────

    public async Task<Result> EnqueueAsync(
        EdgeQueueItem item, int priority = 5, CancellationToken cancellationToken = default)
    {
        using var activity = PersistenceActivitySource.StartQueueEnqueue(item.StudyInstanceUid);
        try
        {
            item.Priority = priority;
            item.Status = TransferStatus.Pending;
            item.CreatedAt = DateTime.UtcNow;

            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            ctx.QueueItems.Add(item);
            await ctx.SaveChangesAsync(cancellationToken);

            metrics.RecordQueueDepth("edge_queue", await GetCountInternal(ctx, cancellationToken));
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogDebug("Enqueued study {StudyUid} with priority {Priority}",
                item.StudyInstanceUid, priority);

            return Result.Success();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Failed to enqueue study {StudyUid}", item.StudyInstanceUid);
            return Result.Failure(new Error("QUEUE_ENQUEUE_FAILED", ex.Message));
        }
    }

    public async Task<Result> EnqueueBatchAsync(
        IEnumerable<EdgeQueueItem> items, int priority = 5, CancellationToken cancellationToken = default)
    {
        var list = items.ToList();
        using var activity = PersistenceActivitySource.StartQueueBatch(list.Count);
        try
        {
            var now = DateTime.UtcNow;
            foreach (var item in list)
            {
                item.Priority = priority;
                item.Status = TransferStatus.Pending;
                item.CreatedAt = now;
            }

            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            ctx.QueueItems.AddRange(list);
            await ctx.SaveChangesAsync(cancellationToken);

            metrics.RecordQueueDepth("edge_queue", await GetCountInternal(ctx, cancellationToken));
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Batch enqueued {Count} items with priority {Priority}", list.Count, priority);

            return Result.Success();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Failed to batch enqueue {Count} items", list.Count);
            return Result.Failure(new Error("QUEUE_BATCH_FAILED", ex.Message));
        }
    }

    // ── Dequeue ───────────────────────────────────────────────────────────────

    public async Task<Result<EdgeQueueItem?>> DequeueAsync(CancellationToken cancellationToken = default)
    {
        using var activity = PersistenceActivitySource.StartQueueDequeue();
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);

            // Release stale locks first (worker crashed without releasing)
            await ReleaseStaleLocksAsync(ctx, cancellationToken);

            var now = DateTime.UtcNow;

            // Atomic: find highest priority unlocked item that is ready
            var item = await ctx.QueueItems
                .Where(q => q.Status == TransferStatus.Pending
                         && !q.IsLocked
                         && (!q.NextRetryAt.HasValue || q.NextRetryAt <= now))
                .OrderBy(q => q.Priority)
                .ThenBy(q => q.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (item is null)
            {
                activity?.SetTag("queue.empty", true);
                return Result<EdgeQueueItem?>.Success(null);
            }

            // Optimistic lock
            item.IsLocked = true;
            item.LockedAt = now;
            item.LockedBy = WorkerId;
            item.Status = TransferStatus.Sending;
            item.LastAttempt = now;
            await ctx.SaveChangesAsync(cancellationToken);

            metrics.RecordQueueDepth("edge_queue", await GetCountInternal(ctx, cancellationToken));
            activity?.SetTag("queue.study_uid", item.StudyInstanceUid);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogDebug("Dequeued study {StudyUid} (priority={Priority}, retry={Retry})",
                item.StudyInstanceUid, item.Priority, item.RetryCount);

            return Result<EdgeQueueItem?>.Success(item);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Failed to dequeue from queue");
            return Result<EdgeQueueItem?>.Failure(new Error("QUEUE_DEQUEUE_FAILED", ex.Message));
        }
    }

    // ── Peek / Inspect ────────────────────────────────────────────────────────

    public async Task<Result<EdgeQueueItem?>> PeekAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            var now = DateTime.UtcNow;

            var item = await ctx.QueueItems.AsNoTracking()
                .Where(q => q.Status == TransferStatus.Pending
                         && !q.IsLocked
                         && (!q.NextRetryAt.HasValue || q.NextRetryAt <= now))
                .OrderBy(q => q.Priority)
                .ThenBy(q => q.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            logger.LogDebug("Queue peek: {Result}",
                item is not null
                    ? $"study={item.StudyInstanceUid}, priority={item.Priority}, age={item.Age}"
                    : "empty");

            return Result<EdgeQueueItem?>.Success(item);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to peek queue");
            return Result<EdgeQueueItem?>.Failure(new Error("QUEUE_PEEK_FAILED", ex.Message));
        }
    }

    public async Task<Result<int>> GetCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            var count = await GetCountInternal(ctx, cancellationToken);
            logger.LogDebug("Queue pending count: {Count}", count);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get queue count");
            return Result<int>.Failure(new Error("QUEUE_COUNT_FAILED", ex.Message));
        }
    }

    public async Task<Result<IEnumerable<EdgeQueueItem>>> GetPendingAsync(
        int count = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            var items = await ctx.QueueItems.AsNoTracking()
                .Where(q => q.Status == TransferStatus.Pending)
                .OrderBy(q => q.Priority)
                .ThenBy(q => q.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            logger.LogDebug("Queue GetPending: returned {Returned}/{Requested} items",
                items.Count, count);

            return Result<IEnumerable<EdgeQueueItem>>.Success(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get pending items");
            return Result<IEnumerable<EdgeQueueItem>>.Failure(
                new Error("QUEUE_PENDING_FAILED", ex.Message));
        }
    }

    public async Task<Result<bool>> IsEmptyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            var any = await ctx.QueueItems
                .AnyAsync(q => q.Status == TransferStatus.Pending, cancellationToken);
            logger.LogDebug("Queue empty check: isEmpty={IsEmpty}", !any);
            return Result<bool>.Success(!any);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check if queue is empty");
            return Result<bool>.Failure(new Error("QUEUE_EMPTY_CHECK_FAILED", ex.Message));
        }
    }

    // ── Retry ─────────────────────────────────────────────────────────────────

    public async Task<Result> RequeueAsync(
        EdgeQueueItem item, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        using var activity = PersistenceActivitySource.StartQueueRequeue(item.StudyInstanceUid);
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            var entity = await ctx.QueueItems.FindAsync([item.Id], cancellationToken);

            if (entity is null)
                return Result.Failure(new Error("QUEUE_ITEM_NOT_FOUND",
                    $"Queue item {item.Id} not found"));

            entity.Status = TransferStatus.Pending;
            entity.RetryCount++;
            entity.NextRetryAt = DateTime.UtcNow.Add(delay);
            entity.IsLocked = false;
            entity.LockedAt = null;
            entity.LockedBy = null;
            entity.ErrorMessage = item.ErrorMessage;

            await ctx.SaveChangesAsync(cancellationToken);

            activity?.SetTag("queue.retry_count", entity.RetryCount);
            activity?.SetTag("queue.next_retry_at", entity.NextRetryAt?.ToString("O"));
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation(
                "Requeued study {StudyUid} for retry #{Retry} in {Delay}",
                entity.StudyInstanceUid, entity.RetryCount, delay);

            return Result.Success();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Failed to requeue item {ItemId}", item.Id);
            return Result.Failure(new Error("QUEUE_REQUEUE_FAILED", ex.Message));
        }
    }

    // ── Remove / Clear ────────────────────────────────────────────────────────

    public async Task<Result> RemoveAsync(
        EdgeQueueItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            var entity = await ctx.QueueItems.FindAsync([item.Id], cancellationToken);

            if (entity is null)
                return Result.Failure(new Error("QUEUE_ITEM_NOT_FOUND",
                    $"Queue item {item.Id} not found"));

            ctx.QueueItems.Remove(entity);
            await ctx.SaveChangesAsync(cancellationToken);

            metrics.RecordQueueDepth("edge_queue", await GetCountInternal(ctx, cancellationToken));
            logger.LogDebug("Removed queue item {ItemId} for study {StudyUid}",
                item.Id, item.StudyInstanceUid);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove queue item {ItemId}", item.Id);
            return Result.Failure(new Error("QUEUE_REMOVE_FAILED", ex.Message));
        }
    }

    public async Task<Result> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(cancellationToken);
            var deleted = await ctx.QueueItems.ExecuteDeleteAsync(cancellationToken);

            metrics.RecordQueueDepth("edge_queue", 0);
            logger.LogWarning("Queue cleared — {Count} items removed", deleted);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clear queue");
            return Result.Failure(new Error("QUEUE_CLEAR_FAILED", ex.Message));
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static async Task<int> GetCountInternal(EdgeNodeDbContext ctx, CancellationToken ct)
        => await ctx.QueueItems.CountAsync(q => q.Status == TransferStatus.Pending, ct);

    private async Task ReleaseStaleLocksAsync(EdgeNodeDbContext ctx, CancellationToken ct)
    {
        var staleCutoff = DateTime.UtcNow.Subtract(StaleLockThreshold);

        var stale = await ctx.QueueItems
            .Where(q => q.IsLocked && q.LockedAt < staleCutoff)
            .ToListAsync(ct);

        if (stale.Count == 0) return;

        foreach (var item in stale)
        {
            item.IsLocked = false;
            item.LockedAt = null;
            item.LockedBy = null;
            item.Status = TransferStatus.Pending;
        }

        await ctx.SaveChangesAsync(ct);
        logger.LogWarning("Released {Count} stale queue locks (threshold={Minutes}min)",
            stale.Count, StaleLockThreshold.TotalMinutes);
    }
}
