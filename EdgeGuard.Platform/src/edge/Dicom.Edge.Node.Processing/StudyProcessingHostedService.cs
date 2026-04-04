using Dicom.Edge.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Processing;

/// <summary>
/// Background service that listens for <c>StudyCompletedEvent</c> and runs each
/// study through the processing pipeline (route → send → notify).
/// </summary>
public sealed class StudyProcessingHostedService(
    IStudyPipeline pipeline,
    IEventBus eventBus,
    ILogger<StudyProcessingHostedService> logger) : BackgroundService
{
    private readonly Queue<string> _pendingStudies = new();
    private readonly SemaphoreSlim _signal = new(0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Study processing hosted service started");

        eventBus.Subscribe<StudyCompletedEvent>(new StudyCompletedHandler(this));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(stoppingToken);

                string? studyUid;
                lock (_pendingStudies)
                {
                    _pendingStudies.TryDequeue(out studyUid);
                }

                if (studyUid is not null)
                {
                    await pipeline.ProcessStudyAsync(studyUid, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Study processing error");
            }
        }
    }

    internal void EnqueueStudy(string studyInstanceUid)
    {
        lock (_pendingStudies)
        {
            _pendingStudies.Enqueue(studyInstanceUid);
        }
        _signal.Release();
    }

    private sealed class StudyCompletedHandler(StudyProcessingHostedService host) : IEventHandler<StudyCompletedEvent>
    {
        public Task HandleAsync(StudyCompletedEvent @event, CancellationToken cancellationToken = default)
        {
            host.EnqueueStudy(@event.StudyInstanceUid);
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Domain event published when a study has completed receiving all instances.
/// </summary>
public sealed class StudyCompletedEvent(string studyInstanceUid) : IEdgeEvent
{
    public string StudyInstanceUid { get; } = studyInstanceUid;
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
    public string EventType => "StudyCompleted";
    public string Source => "Node.Processing";
    public string? CorrelationId => null;
    public int Version => 1;
}
