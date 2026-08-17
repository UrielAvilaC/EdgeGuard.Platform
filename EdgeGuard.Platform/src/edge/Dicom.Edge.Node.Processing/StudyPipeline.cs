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
        // P0-4: Build a fully-populated routing context from the work item so that
        // routing rules with Modality/SourceAe/Institution/InstanceCount conditions
        // can actually match. Previously every field was null → all rules inert.
        var context = new StudyRoutingContext
        {
            StudyInstanceUid = studyInstanceUid,
            Modality         = workItem.Modality?.Trim().ToUpperInvariant(),
            SourceAeTitle    = workItem.SourceAeTitle?.Trim(),
            InstitutionName  = workItem.InstitutionName?.Trim(),
            StudyDescription = workItem.StudyDescription?.Trim(),
            AccessionNumber  = workItem.AccessionNumber?.Trim(),
            InstanceCount    = workItem.InstanceCount,
            Priority         = workItem.Priority,
        };
        // Manual resend from the Hub carries explicit PACS targets and must bypass
        // routing rules entirely — the operator already chose exactly where to send.
        var destinations = workItem.ExplicitPacsIds is { Count: > 0 } explicitPacsIds
            ? await router.ResolveExplicitDestinationsAsync(explicitPacsIds, ct)
            : await router.ResolveDestinationsAsync(context, ct);

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
            patientBirthDate: workItem.PatientBirthDate,
            patientSex:       workItem.PatientSex,
            ct:               ct);

        // Await the completion notification first so the study exists on the Hub
        // before we report the PACS-send phases (Enviando / Enviado a PACS).
        var hubNotified = false;
        try
        {
            hubNotified = await hubNotifyTask;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Hub notification failed for study {StudyUid} — pipeline continues", studyInstanceUid);
        }

        if (destinations.Count > 0)
        {
            // Primary destination AE for status display (Hub tracks a single send status).
            var primaryAeTitle = destinations[0].AeTitle;

            // ── Enviando a PACS ────────────────────────────────────────────────
            await hubNotifier.NotifyPacsSendStatusAsync(
                nodeId:            string.Empty,
                studyInstanceUid:  studyInstanceUid,
                status:            "Sending",
                targetPacsAeTitle: primaryAeTitle,
                ct:                ct);

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

            // ── Enviado a PACS / Failed ────────────────────────────────────────
            // Mark sent when at least one destination accepted the study; otherwise failed.
            await hubNotifier.NotifyPacsSendStatusAsync(
                nodeId:            string.Empty,
                studyInstanceUid:  studyInstanceUid,
                status:            sent > 0 ? "SentToPacs" : "Failed",
                targetPacsAeTitle: primaryAeTitle,
                error:             sent > 0 ? null : string.Join("; ", errors),
                ct:                ct);
        }
        else
        {
            // Nothing resolved. The Hub put this study in QueuedForSend when it requested the
            // resend and only ever leaves that state on a PACS-status report — returning here
            // without one strands it there forever. Report the terminal status so the operator
            // sees a failure with a reason instead of a study stuck "queued".
            await hubNotifier.NotifyPacsSendStatusAsync(
                nodeId:            string.Empty,
                studyInstanceUid:  studyInstanceUid,
                status:            "Failed",
                targetPacsAeTitle: null,
                error:             string.Join("; ", errors),
                ct:                ct);
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

