using System.Diagnostics;
using System.Threading.Channels;
using Dicom.Edge.Abstractions.Context;
using Dicom.Edge.Abstractions.Events;
using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Node.Persistence.Diagnostics;

namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Background service that detects when a DICOM study has finished being received.
/// <para>
/// <strong>Strategy:</strong> Inactivity-based detection — if no new instance has arrived
/// for a study within <c>dicom.study_completion_timeout_sec</c>, the study is marked
/// <see cref="StudyStatus.Completed"/> and a <see cref="StudyCompletedEvent"/> is published.
/// </para>
/// <para>
/// Polling interval = timeout / 2 (capped to a minimum of 5 s) to ensure timely detection
/// without excessive DB queries.
/// An immediate check can also be triggered via <see cref="IStudyCompletionTrigger.RequestImmediateCheck"/>
/// (e.g., on DICOM association release) to reduce latency for the common single-association case.
/// </para>
/// </summary>
public sealed class StudyCompletionWatcherService(
    IDbContextFactory<EdgeNodeDbContext> factory,
    INodeSettingsService settings,
    IEventBus eventBus,
    IMetricsCollector metrics,
    ILogger<StudyCompletionWatcherService> logger) : BackgroundService, IStudyCompletionTrigger
{
    // Bounded to 1: multiple rapid triggers (e.g., concurrent associations releasing)
    // collapse into a single scan instead of queuing redundant checks.
    private readonly Channel<bool> _triggerChannel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    /// <inheritdoc />
    public void RequestImmediateCheck() => _triggerChannel.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StudyCompletionWatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Re-read config each cycle so Hub-pushed changes take effect immediately
                var dicom = await settings.GetDicomConfigAsync(stoppingToken);
                var pollInterval = TimeSpan.FromSeconds(Math.Max(5, dicom.StudyCompletionTimeoutSec / 2));

                await DetectCompletedStudiesAsync(stoppingToken);

                // Wait for the next poll interval OR an early trigger from association release
                await Task.WhenAny(
                    Task.Delay(pollInterval, stoppingToken),
                    _triggerChannel.Reader.WaitToReadAsync(stoppingToken).AsTask());

                // Drain any pending signals to avoid back-to-back redundant scans
                while (_triggerChannel.Reader.TryRead(out _)) { }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
                metrics.RecordError("StudyCompletionWatcher", ex.Message, severity: 2);
                logger.LogError(ex, "Unhandled error in completion watcher — retrying in 15s");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task DetectCompletedStudiesAsync(CancellationToken ct)
    {
        using var scanActivity = PersistenceActivitySource.StartCompletionScan();

        var dicom = await settings.GetDicomConfigAsync(ct);
        var cutoff = DateTime.UtcNow.AddSeconds(-dicom.StudyCompletionTimeoutSec);

        await using var ctx = await factory.CreateDbContextAsync(ct);

        var ready = await ctx.Studies
            .Where(s => s.Status == StudyStatus.Receiving
                     && s.LastImageReceivedAt < cutoff)
            .ToListAsync(ct);

        scanActivity?.SetTag("persistence.scan.found", ready.Count);

        if (ready.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var study in ready)
        {
            using var studyActivity = PersistenceActivitySource.StartStudyComplete(study.StudyInstanceUid);

            study.Status = StudyStatus.Completed;
            ctx.Entry(study).Property<DateTime>("updated_at").CurrentValue = now;

            // ── Persist study metrics (one record per study, guarded by unique index) ──
            var metricsExist = await ctx.Metrics
                .AnyAsync(m => m.StudyInstanceUid == study.StudyInstanceUid, ct);

            if (!metricsExist)
            {
                ctx.Metrics.Add(new StudyMetrics
                {
                    StudyInstanceUid  = study.StudyInstanceUid,
                    TotalSizeBytes    = study.TotalSizeBytes,
                    InstancesReceived = study.InstanceCount,
                    InstancesFailed   = 0,
                    FirstImageAt      = study.ReceivedAt,
                    LastImageAt       = study.LastImageReceivedAt,
                    ReceptionDuration = study.LastImageReceivedAt - study.ReceivedAt,
                    AverageImageSize  = study.InstanceCount > 0
                        ? (double)study.TotalSizeBytes / study.InstanceCount
                        : 0,
                });
            }

            await eventBus.PublishAsync(new StudyCompletedEvent(
                new StudyContext
                {
                    StudyInstanceUid = study.StudyInstanceUid,
                    InstanceCount    = study.InstanceCount,
                    CallingAeTitle   = study.SourceAeTitle ?? string.Empty,
                    CompletedAt      = now,
                }), ct);

            metrics.RecordStudyReceived(
                study.StudyInstanceUid,
                study.TotalSizeBytes,
                study.InstanceCount);

            studyActivity?.SetTag("dicom.instance.count", study.InstanceCount);
            studyActivity?.SetTag("dicom.size.bytes", study.TotalSizeBytes);
            studyActivity?.SetStatus(ActivityStatusCode.Ok);

            logger.LogInformation(
                "Study {StudyUid} completed — {Instances} instances, {SizeMb:F1} MB",
                study.StudyInstanceUid,
                study.InstanceCount,
                study.TotalSizeBytes / (1024.0 * 1024.0));
        }

        await ctx.SaveChangesAsync(ct);
        scanActivity?.SetStatus(ActivityStatusCode.Ok);
        logger.LogDebug("Marked {Count} studies as Completed", ready.Count);
    }
}
