using System.Diagnostics;
using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Abstractions.Monitoring;
using Dicom.Edge.Abstractions.Storage;
using Dicom.Edge.Node.Queue;
using Dicom.Edge.Node.Router;
using Dicom.Edge.Node.Sender;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Processing;

/// <summary>
/// Study processing pipeline. Resolves PACS destinations, sends in parallel,
/// then notifies the Hub with study metadata for SPA representation.
/// </summary>
public sealed class StudyPipeline(
    IStudyRouter router,
    IPacsSender pacsSender,
    IStudyHubNotifier hubNotifier,
    IMetricsCollector metrics,
    
    ILogger<StudyPipeline> logger) : IStudyPipeline
{
    public async Task<StudyPipelineResult> ProcessStudyAsync(
        NodeWorkItem workItem, CancellationToken ct = default)
    {
        var studyInstanceUid = workItem.StudyInstanceUid;
        var sw = Stopwatch.StartNew();
        var errors = new List<string>();

        logger.LogInformation("Pipeline starting for study {StudyUid}", studyInstanceUid);

        // ── 1. Resolve destinations ───────────────────────────────────────────
        var context = new StudyRoutingContext { StudyInstanceUid = studyInstanceUid };
        var destinations = await router.ResolveDestinationsAsync(context, ct);

        if (destinations.Count == 0)
        {
            errors.Add("No PACS destination resolved");
            logger.LogWarning("No destination for study {StudyUid} — skipping PACS send", studyInstanceUid);
        }

        // ── 2+3. Send to all PACS destinations AND notify Hub in parallel ─────
        var sent   = 0;
        var failed = 0;

        // Start Hub notification immediately so it runs concurrently with PACS sends
        logger.LogInformation("Notifying Hub of study completion for {StudyUid}", studyInstanceUid);
        var hubNotifyTask = hubNotifier.NotifyStudyCompletedAsync(
            nodeId:           string.Empty, // resolved inside notifier from registered client
            studyInstanceUid: studyInstanceUid,
            patientId:        workItem.PatientId,
            patientName:      workItem.PatientName,
            accessionNumber:  workItem.AccessionNumber,
            instanceCount:    workItem.InstanceCount,
            totalSizeBytes:   workItem.TotalSizeBytes,
            studyDate:        workItem.StudyDate,
            studyDescription: workItem.StudyDescription,
            seriesCount:      workItem.SeriesCount,
            ct:               ct);

        if (destinations.Count > 0)
        {
            var sendTasks = destinations.Select(async dest =>
            {
                logger.LogInformation("Sending study {StudyUid} to {AeTitle} ({Host}:{Port})", studyInstanceUid, dest.AeTitle, dest.Host, dest.Port);
                var result = await pacsSender.SendStudyAsync(studyInstanceUid, dest, ct);
                if (result.Success)
                {
                    Interlocked.Increment(ref sent);
                    metrics.RecordTransferCompleted(studyInstanceUid, result.Duration, true);
                }
                else
                {
                    Interlocked.Increment(ref failed);
                    errors.Add($"{dest.AeTitle}: {result.Error}");
                    metrics.RecordTransferCompleted(studyInstanceUid, result.Duration, false);
                }
            });

            await Task.WhenAll(sendTasks);
        }

        // Await Hub result (was running in parallel with PACS sends)
        var hubNotified = false;
        try
        {
            hubNotified = await hubNotifyTask;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Hub notification failed for study {StudyUid} — pipeline continues", studyInstanceUid);
        }

        sw.Stop();

        logger.LogInformation(
            "Pipeline completed for {StudyUid}: {Sent} sent, {Failed} failed, HubNotified={HubNotified} in {Duration:N1}s",
            studyInstanceUid, sent, failed, hubNotified, sw.Elapsed.TotalSeconds);

        return new StudyPipelineResult
        {
            StudyInstanceUid  = studyInstanceUid,
            RoutingSuccess    = destinations.Count > 0,
            DestinationsSent  = sent,
            DestinationsFailed = failed,
            HubNotified       = hubNotified,
            TotalDuration     = sw.Elapsed,
            Errors            = errors,
        };
    }
}

