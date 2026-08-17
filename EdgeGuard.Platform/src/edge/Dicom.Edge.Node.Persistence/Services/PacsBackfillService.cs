using System.Threading.Channels;
using Dicom.Edge.Node.Persistence.Repositories;
using Dicom.Edge.Node.Queue;
using Dicom.Edge.Node.Router;

namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Requests a historical backfill for PACS destinations that were just added to this node.
/// </summary>
public interface IPacsBackfillTrigger
{
    /// <summary>
    /// Queues a backfill run for the given <c>NodePacsServer.Id</c> values. Returns immediately;
    /// the scan runs on the background service so the Hub sync endpoint is not blocked.
    /// </summary>
    void RequestBackfill(IReadOnlyList<string> newPacsIds);
}

/// <summary>
/// When the Hub assigns a <b>new</b> PACS to this node, historical studies that were received
/// before the assignment have never been sent there. This service replays them.
///
/// <para><b>Trigger:</b> <see cref="RequestBackfill"/>, called by the PACS sync endpoint with the
/// ids that were not present in <c>node_pacs_servers</c> before the sync.</para>
///
/// <para><b>Selection:</b> for every study received within the lookback window the routing rules
/// are re-evaluated exactly as they would be for a fresh study (<see cref="IStudyRouter"/>). A study
/// is replayed only when the new PACS is among the resolved destinations — i.e. only studies that
/// <em>match the rules</em> for that destination. Rules are reloaded first so a PACS that arrived in
/// the same sync is already resolvable.</para>
///
/// <para><b>Enqueue:</b> one work item per (study, new PACS) pair, carrying
/// <see cref="NodeWorkItem.ExplicitPacsIds"/> so the pipeline sends only to the new destination and
/// does not re-send to PACS that already hold the study. Items are enqueued at a low priority
/// (higher number, see <c>backfill.priority</c>) so live traffic always drains first.</para>
///
/// <para><b>Settings</b> (<c>node_settings</c>): <c>backfill.enabled</c>,
/// <c>backfill.lookback_days</c>, <c>backfill.max_studies</c>, <c>backfill.priority</c>.</para>
/// </summary>
public sealed class PacsBackfillService(
    IDbContextFactory<EdgeNodeDbContext> factory,
    INodeSettingsService settings,
    INodePacsServerRepository pacsRepository,
    RoutingRuleLoaderService ruleLoader,
    RuleBasedStudyRouter router,
    INodeWorkQueue workQueue,
    ILogger<PacsBackfillService> logger) : BackgroundService, IPacsBackfillTrigger
{
    /// <summary>
    /// Only studies whose reception finished are replayable — a study still being received
    /// is picked up by the normal completion path.
    /// </summary>
    private static readonly StudyStatus[] ReplayableStatuses =
    [
        StudyStatus.Completed,
        StudyStatus.WaitingForImageLinks,
        StudyStatus.WaitingForReport,
        StudyStatus.Finalized,
        StudyStatus.QueuedForSend,
        StudyStatus.SentToPacs,
        StudyStatus.Failed,
    ];

    // Unbounded: each request carries a distinct set of PACS ids and must not be dropped.
    private readonly Channel<IReadOnlyList<string>> _requests =
        Channel.CreateUnbounded<IReadOnlyList<string>>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });

    /// <inheritdoc />
    public void RequestBackfill(IReadOnlyList<string> newPacsIds)
    {
        if (newPacsIds.Count == 0) return;

        if (_requests.Writer.TryWrite(newPacsIds))
        {
            logger.LogInformation(
                "Backfill requested for {Count} newly assigned PACS: [{PacsIds}]",
                newPacsIds.Count, string.Join(", ", newPacsIds));
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PacsBackfillService started");

        await foreach (var pacsIds in _requests.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await RunBackfillAsync(pacsIds, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Backfill run failed for PACS [{PacsIds}]", string.Join(", ", pacsIds));
            }
        }

        logger.LogInformation("PacsBackfillService stopped");
    }

    private async Task RunBackfillAsync(IReadOnlyList<string> newPacsIds, CancellationToken ct)
    {
        if (!await settings.GetAsync(NodeSettingKeys.PacsBackfill.Enabled, true, ct))
        {
            logger.LogInformation("Backfill skipped — backfill.enabled=false");
            return;
        }

        var lookbackDays = await settings.GetAsync(NodeSettingKeys.PacsBackfill.LookbackDays, 30, ct);
        var maxStudies   = await settings.GetAsync(NodeSettingKeys.PacsBackfill.MaxStudies, 1000, ct);
        var priority     = await settings.GetAsync(NodeSettingKeys.PacsBackfill.Priority, 9, ct);

        // The new PACS arrived in the sync that triggered this run, so the router still holds
        // the previous destination set. Reload before resolving or nothing would match.
        await ruleLoader.LoadNowAsync(ct);

        // Keep only ids that actually resolve to an enabled destination on this node.
        var enabled = await pacsRepository.GetAllEnabledAsync(ct);
        var targets = newPacsIds
            .Where(id => enabled.Any(p => p.Id == id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (targets.Count == 0)
        {
            logger.LogInformation(
                "Backfill skipped — none of the requested PACS [{PacsIds}] is enabled on this node",
                string.Join(", ", newPacsIds));
            return;
        }

        await using var ctx = await factory.CreateDbContextAsync(ct);

        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(lookbackDays));

        // The DicomStudy query filter already excludes soft-deleted studies — their files are
        // gone from disk, so replaying them would only produce failures.
        var studies = await ctx.Studies
            .Where(s => ReplayableStatuses.Contains(s.Status) && s.ReceivedAt >= cutoff)
            .OrderByDescending(s => s.ReceivedAt)
            .Take(maxStudies)
            .AsNoTracking()
            .ToListAsync(ct);

        if (studies.Count == 0)
        {
            logger.LogInformation(
                "Backfill found no studies received since {Cutoff:u} — nothing to replay", cutoff);
            return;
        }

        var studyUids = studies.Select(s => s.StudyInstanceUid).ToList();

        // Modality lives on the series, not the study — resolve one per study for rule matching.
        var modalities = await ctx.Series
            .Where(s => studyUids.Contains(s.StudyInstanceUid))
            .GroupBy(s => s.StudyInstanceUid)
            .Select(g => new { StudyUid = g.Key, Modality = g.Min(x => x.Modality) })
            .ToDictionaryAsync(x => x.StudyUid, x => x.Modality, ct);

        var seriesCounts = await ctx.Series
            .Where(s => studyUids.Contains(s.StudyInstanceUid))
            .GroupBy(s => s.StudyInstanceUid)
            .Select(g => new { StudyUid = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudyUid, x => x.Count, ct);

        // Already-queued (study, destination) pairs — a rerun of the same sync, or a backfill
        // still draining, must not enqueue the same work twice.
        var alreadyQueued = await ctx.QueueItems
            .Where(q => studyUids.Contains(q.StudyInstanceUid) && targets.Contains(q.Destination))
            .Select(q => new { q.StudyInstanceUid, q.Destination })
            .ToListAsync(ct);

        var queuedPairs = alreadyQueued
            .Select(q => $"{q.StudyInstanceUid}|{q.Destination}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var enqueued = 0;
        var skipped  = 0;

        foreach (var study in studies)
        {
            ct.ThrowIfCancellationRequested();

            var context = new StudyRoutingContext
            {
                StudyInstanceUid = study.StudyInstanceUid,
                Modality         = modalities.GetValueOrDefault(study.StudyInstanceUid)?.Trim().ToUpperInvariant(),
                SourceAeTitle    = study.SourceAeTitle?.Trim(),
                StudyDescription = study.StudyDescription?.Trim(),
                AccessionNumber  = study.AccessionNumber?.Trim(),
                InstanceCount    = study.InstanceCount,
                Priority         = priority,
            };

            // Same resolution a freshly received study would get, so "matches the rules"
            // means exactly what it means for live traffic (including the default-destination
            // fallback when no rule matches).
            var destinations = await router.ResolveDestinationsAsync(context, ct);

            var matchedTargets = destinations
                .Select(d => d.Id)
                .Where(targets.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matchedTargets.Count == 0)
            {
                skipped++;
                continue;
            }

            foreach (var pacsId in matchedTargets)
            {
                if (!queuedPairs.Add($"{study.StudyInstanceUid}|{pacsId}"))
                    continue;

                var workItem = new NodeWorkItem
                {
                    Id               = Guid.NewGuid().ToString(),
                    StudyInstanceUid = study.StudyInstanceUid,
                    Type             = NodeWorkItemType.PacsSend,
                    Priority         = priority,
                    SourceAeTitle    = study.SourceAeTitle,
                    TargetPacsId     = pacsId,
                    ExplicitPacsIds  = [pacsId],
                    CreatedAt        = DateTime.UtcNow,
                    PatientId        = study.PatientId,
                    PatientName      = study.PatientName,
                    AccessionNumber  = study.AccessionNumber,
                    TotalSizeBytes   = study.TotalSizeBytes,
                    InstanceCount    = study.InstanceCount,
                    StudyDate        = study.StudyDate,
                    StudyDescription = study.StudyDescription,
                    SeriesCount      = seriesCounts.GetValueOrDefault(study.StudyInstanceUid, 0),
                    Modality         = context.Modality,
                };

                var result = await workQueue.EnqueueAsync(workItem, ct);
                if (result.IsSuccess)
                {
                    enqueued++;
                }
                else
                {
                    logger.LogWarning(
                        "Backfill could not enqueue study {StudyUid} for PACS {PacsId}: {Error}",
                        study.StudyInstanceUid, pacsId, result.Error?.Message);
                }
            }
        }

        logger.LogInformation(
            "Backfill complete for PACS [{PacsIds}]: {Enqueued} work item(s) enqueued from " +
            "{Scanned} study/studies since {Cutoff:u} ({Skipped} did not match the routing rules)",
            string.Join(", ", targets), enqueued, studies.Count, cutoff, skipped);
    }
}
