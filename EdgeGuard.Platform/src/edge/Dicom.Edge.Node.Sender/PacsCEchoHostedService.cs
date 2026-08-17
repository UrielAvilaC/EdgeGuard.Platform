using System.Collections.Concurrent;
using Dicom.Edge.Abstractions.Monitoring;
using Dicom.Edge.Contracts.Node;
using Dicom.Edge.Node.Queue;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.Sender;

/// <summary>
/// Periodic BackgroundService that runs DICOM C-ECHO against all configured
/// PACS destinations and exposes results via <see cref="IPacsCEchoMonitor"/>.
/// Uses <see cref="IOptionsMonitor{T}"/> so that destinations pushed from the Hub
/// via configuration apply are picked up without restarting the service.
/// </summary>
/// <remarks>
/// Also acts as the reconnection trigger for the node's outbox: when a destination
/// transitions from failing to succeeding, previously <c>Failed</c> queue items for
/// that destination are requeued (see <see cref="INodeWorkQueue.RequeueFailedAsync"/>)
/// so studies that failed while the PACS was down are automatically resent once it is
/// back online, instead of sitting in <c>Failed</c> until someone retries manually.
/// </remarks>
public sealed class PacsCEchoHostedService(
    IPacsSender pacsSender,
    IPacsEchoHubReporter hubReporter,
    INodeWorkQueue workQueue,
    IOptionsMonitor<PacsCEchoOptions> optionsMonitor,
    ILogger<PacsCEchoHostedService> logger) : BackgroundService, IPacsCEchoMonitor
{
    private readonly ConcurrentDictionary<string, PacsCEchoResultDto> _results = new();

    /// <summary>Last known success/failure per destination AE title, used to detect reconnection edges.</summary>
    private readonly ConcurrentDictionary<string, bool> _lastKnownUp = new();

    /// <summary>Always reads the latest options snapshot — reflects Hub config pushes.</summary>
    private PacsCEchoOptions Opts => optionsMonitor.CurrentValue;

    public IReadOnlyList<PacsCEchoResultDto> GetLatestResults() =>
        _results.Values.ToList().AsReadOnly();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PACS C-ECHO monitor started — waiting for destinations");

        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = Opts;

            if (!opts.Enabled)
            {
                logger.LogInformation("PACS C-ECHO monitor disabled — sleeping 60s");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                continue;
            }

            if (opts.Destinations.Length == 0)
            {
                logger.LogInformation("PACS C-ECHO — no destinations configured yet, retrying in 30s");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                continue;
            }

            logger.LogInformation(
                "PACS C-ECHO checking {Count} destination(s) — next check in {Interval}s",
                opts.Destinations.Length, opts.IntervalSeconds);

            await RunAllChecksAsync(opts, stoppingToken);

            // Push results to Hub so the SPA can display per-node connectivity status
            await hubReporter.ReportAsync(GetLatestResults(), stoppingToken);
            logger.LogInformation("PACS C-ECHO checks complete — sleeping {Interval}s", opts.IntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(opts.IntervalSeconds), stoppingToken);
        }
    }

    private async Task RunAllChecksAsync(PacsCEchoOptions opts, CancellationToken ct)
    {
        foreach (var destination in opts.Destinations)
        {
            try
            {
                var echoResult = await pacsSender.VerifyConnectionAsync(destination, ct);

                var result = new PacsCEchoResultDto
                {
                    DestinationAeTitle = destination.AeTitle,
                    Host               = destination.Host,
                    Port               = destination.Port,
                    Success            = echoResult.Success,
                    CheckedAtUtc       = DateTime.UtcNow,
                    LatencyMs          = echoResult.LatencyMs,
                    Error              = echoResult.ErrorMessage,
                    ErrorReason        = echoResult.ErrorReason,
                };

                _results[destination.AeTitle] = result;

                if (echoResult.Success)
                {
                    logger.LogDebug(
                        "C-ECHO to {AeTitle}@{Host}:{Port} succeeded in {Latency:N1}ms",
                        destination.AeTitle, destination.Host, destination.Port, echoResult.LatencyMs);
                    await HandleReconnectionAsync(destination.AeTitle, ct);
                }
                else
                {
                    logger.LogWarning(
                        "C-ECHO to {AeTitle}@{Host}:{Port} failed (no DICOM success status) — Reason={Reason}",
                        destination.AeTitle, destination.Host, destination.Port, echoResult.ErrorReason ?? echoResult.ErrorMessage);
                    _lastKnownUp[destination.AeTitle] = false;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _results[destination.AeTitle] = new PacsCEchoResultDto
                {
                    DestinationAeTitle = destination.AeTitle,
                    Host               = destination.Host,
                    Port               = destination.Port,
                    Success            = false,
                    CheckedAtUtc       = DateTime.UtcNow,
                    Error              = ex.Message,
                };
                _lastKnownUp[destination.AeTitle] = false;

                logger.LogWarning(ex,
                    "C-ECHO check failed for {AeTitle}@{Host}:{Port}",
                    destination.AeTitle, destination.Host, destination.Port);
            }
        }
    }

    /// <summary>
    /// Detects a failing→succeeding transition for <paramref name="aeTitle"/> and, when one
    /// occurs, requeues studies that previously failed against that destination so they are
    /// automatically resent now that the PACS is reachable again.
    /// </summary>
    private async Task HandleReconnectionAsync(string aeTitle, CancellationToken ct)
    {
        // GetOrAdd seeds "true" on first observation so process startup (PACS already up)
        // does not look like a reconnection and trigger a spurious requeue.
        var wasUp = _lastKnownUp.GetOrAdd(aeTitle, true);
        _lastKnownUp[aeTitle] = true;

        if (wasUp)
            return;

        var result = await workQueue.RequeueFailedAsync(aeTitle, ct);
        if (result.IsSuccess && result.Value > 0)
        {
            logger.LogInformation(
                "PACS {AeTitle} back online — requeued {Count} previously failed stud(ies) for retry",
                aeTitle, result.Value);
        }
        else if (result.IsFailure)
        {
            logger.LogWarning(
                "PACS {AeTitle} back online but requeue of failed items errored: {Error}",
                aeTitle, result.Error?.Message);
        }
    }
}
