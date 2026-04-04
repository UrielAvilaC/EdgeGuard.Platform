using System.Diagnostics;
using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Node.Router;
using Dicom.Edge.Node.Sender;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Processing;

/// <summary>
/// Enterprise study processing pipeline. Coordinates routing, PACS send, and metrics.
/// </summary>
public sealed class StudyPipeline(
    IStudyRouter router,
    IPacsSender pacsSender,
    IMetricsCollector metrics,
    ILogger<StudyPipeline> logger) : IStudyPipeline
{
    public async Task<StudyPipelineResult> ProcessStudyAsync(
        string studyInstanceUid, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var errors = new List<string>();
        var sent = 0;
        var failed = 0;

        logger.LogInformation("Pipeline starting for study {StudyUid}", studyInstanceUid);

        // 1. Resolve destinations
        var context = new StudyRoutingContext { StudyInstanceUid = studyInstanceUid };
        var destinations = await router.ResolveDestinationsAsync(context, ct);

        if (destinations.Count == 0)
        {
            errors.Add("No PACS destination resolved");
            logger.LogWarning("No destination for study {StudyUid} — pipeline aborted", studyInstanceUid);
        }

        // 2. Send to each destination
        foreach (var dest in destinations)
        {
            var result = await pacsSender.SendStudyAsync(studyInstanceUid, dest, ct);
            if (result.Success)
            {
                sent++;
                metrics.RecordTransferCompleted(studyInstanceUid, result.Duration, true);
            }
            else
            {
                failed++;
                errors.Add($"{dest.AeTitle}: {result.Error}");
                metrics.RecordTransferCompleted(studyInstanceUid, result.Duration, false);
            }
        }

        sw.Stop();

        logger.LogInformation(
            "Pipeline completed for {StudyUid}: {Sent} sent, {Failed} failed in {Duration:N1}s",
            studyInstanceUid, sent, failed, sw.Elapsed.TotalSeconds);

        return new StudyPipelineResult
        {
            StudyInstanceUid = studyInstanceUid,
            RoutingSuccess = destinations.Count > 0,
            DestinationsSent = sent,
            DestinationsFailed = failed,
            TotalDuration = sw.Elapsed,
            Errors = errors
        };
    }
}
