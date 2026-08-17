using Dicom.Edge.Abstractions.Equipment;
using Dicom.Edge.Contracts.Hub;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Periodic BackgroundService that reports equipment presence (last-seen) deltas to the Hub.
/// Reads the in-memory <see cref="IEquipmentActivityTracker"/> populated by the DICOM SCP on
/// accepted associations and forwards only entries whose last-seen advanced since the previous
/// report. Never throws — a failed report is retried on the next cycle.
/// </summary>
public sealed class EquipmentStatusReportHostedService(
    IHubSyncClient hubClient,
    IEquipmentActivityTracker activityTracker,
    IOptionsMonitor<EquipmentPresenceOptions> optionsMonitor,
    ILogger<EquipmentStatusReportHostedService> logger) : BackgroundService
{
    private readonly Dictionary<string, DateTime> _lastReported = new(StringComparer.OrdinalIgnoreCase);

    private EquipmentPresenceOptions Opts => optionsMonitor.CurrentValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Equipment presence reporter started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = Opts;
            var interval = TimeSpan.FromSeconds(Math.Max(15, opts.IntervalSeconds));

            try
            {
                if (opts.Enabled)
                    await ReportDeltaAsync(stoppingToken);

                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Equipment presence report cycle failed — retrying in {Interval}", interval);
                try { await Task.Delay(interval, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        logger.LogInformation("Equipment presence reporter stopped");
    }

    private async Task ReportDeltaAsync(CancellationToken ct)
    {
        var nodeId = hubClient.RegisteredNodeId;
        if (string.IsNullOrEmpty(nodeId))
        {
            logger.LogDebug("Skipping equipment presence report — node not yet registered");
            return;
        }

        var snapshot = activityTracker.GetSnapshot();

        var delta = snapshot
            .Where(kv => !_lastReported.TryGetValue(kv.Key, out var prev) || kv.Value > prev)
            .Select(kv => new EquipmentStatusEntry { AeTitle = kv.Key, LastSeenUtc = kv.Value })
            .ToList();

        if (delta.Count == 0)
            return;

        var request = new NodeEquipmentStatusReportRequest
        {
            NodeId        = nodeId,
            ReportedAtUtc = DateTime.UtcNow,
            Equipment     = delta,
        };

        var success = await hubClient.ReportEquipmentStatusAsync(request, ct);
        if (success)
        {
            foreach (var entry in delta)
                _lastReported[entry.AeTitle] = entry.LastSeenUtc;

            logger.LogDebug("Equipment presence reported to Hub — {Count} equipment", delta.Count);
        }
        else
        {
            logger.LogWarning("Equipment presence report not acknowledged — will retry next cycle");
        }
    }
}
