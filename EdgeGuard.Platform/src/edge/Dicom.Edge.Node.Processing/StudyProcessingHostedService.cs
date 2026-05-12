using Dicom.Edge.Abstractions.Events;
using Dicom.Edge.Node.Queue;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Processing;

/// <summary>
/// Background service that polls the persistent <see cref="INodeWorkQueue"/> for completed
/// studies and runs each through the processing pipeline (route → send → notify).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Architecture:</strong> The <see cref="StudyCompletionEnqueueHandler"/> listens for
/// <see cref="Abstractions.Events.StudyCompletedEvent"/> (published by the Persistence layer's
/// <c>StudyCompletionWatcherService</c>) and writes a durable <see cref="NodeWorkItem"/> into the
/// SQLite-backed work queue. This service then dequeues and processes items, ensuring nothing
/// is lost on crash or restart.
/// </para>
/// </remarks>
public sealed class StudyProcessingHostedService(
    IStudyPipeline pipeline,
    INodeWorkQueue workQueue,
    IEventBus eventBus,
    ILogger<StudyProcessingHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Interval between queue polls when the queue is empty.
    /// </summary>
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Short delay between consecutive dequeues to avoid tight-looping.
    /// </summary>
    private static readonly TimeSpan BusyPollInterval = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Study processing hosted service started (persistent queue mode)");

        // Subscribe to completion events to enqueue work items into the persistent queue
        eventBus.Subscribe<StudyCompletedEvent>(
            new StudyCompletionEnqueueHandler(workQueue, logger));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dequeueResult = await workQueue.DequeueAsync(stoppingToken);

                if (dequeueResult.IsFailure)
                {
                    logger.LogWarning("Queue dequeue failed: {Error}", dequeueResult.Error?.Message);
                    await Task.Delay(IdlePollInterval, stoppingToken);
                    continue;
                }

                var workItem = dequeueResult.Value;
                if (workItem is null)
                {
                    // Queue is empty — wait before polling again
                    await Task.Delay(IdlePollInterval, stoppingToken);
                    continue;
                }

                logger.LogInformation(
                    "Processing work item {ItemId} for study {StudyUid} (type={Type}, retry={Retry})",
                    workItem.Id, workItem.StudyInstanceUid, workItem.Type, workItem.RetryCount);

                await pipeline.ProcessStudyAsync(workItem, stoppingToken);

                // Small delay between items to avoid monopolizing the DB
                await Task.Delay(BusyPollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Study processing error — retrying after delay");
                await Task.Delay(IdlePollInterval, stoppingToken);
            }
        }

        logger.LogInformation("Study processing hosted service stopped");
    }
}

/// <summary>
/// Event handler that writes a durable <see cref="NodeWorkItem"/> into the persistent
/// work queue when a <see cref="StudyCompletedEvent"/> is raised by the persistence layer.
/// </summary>
internal sealed class StudyCompletionEnqueueHandler(
    INodeWorkQueue workQueue,
    ILogger logger) : IEventHandler<StudyCompletedEvent>
{
    public async Task HandleAsync(StudyCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        var workItem = new NodeWorkItem
        {
            Id               = Guid.NewGuid().ToString(),
            StudyInstanceUid = @event.Study.StudyInstanceUid,
            Type             = NodeWorkItemType.PacsSend,
            Priority         = 5,
            SourceAeTitle    = @event.Study.CallingAeTitle,
            CreatedAt        = DateTime.UtcNow,
            PatientId        = @event.Study.PatientId,
            PatientName      = @event.Study.PatientName,
            AccessionNumber  = @event.Study.AccessionNumber,
            TotalSizeBytes   = @event.Study.TotalSizeBytes,
            InstanceCount    = @event.Study.InstanceCount,
        };

        var result = await workQueue.EnqueueAsync(workItem, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Enqueued work item for completed study {StudyUid}",
                @event.Study.StudyInstanceUid);
        }
        else
        {
            logger.LogError(
                "Failed to enqueue work item for study {StudyUid}: {Error}",
                @event.Study.StudyInstanceUid, result.Error?.Message);
        }
    }
}
