using System.Collections.Concurrent;
using System.Diagnostics;
using Dicom.Edge.Abstractions.Monitoring;
using Dicom.Edge.Contracts.Node;
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
public sealed class PacsCEchoHostedService(
    IPacsSender pacsSender,
    IOptionsMonitor<PacsCEchoOptions> optionsMonitor,
    ILogger<PacsCEchoHostedService> logger) : BackgroundService, IPacsCEchoMonitor
{
    private readonly ConcurrentDictionary<string, PacsCEchoResultDto> _results = new();

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
                logger.LogDebug("PACS C-ECHO monitor disabled — sleeping 60s");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                continue;
            }

            if (opts.Destinations.Length == 0)
            {
                logger.LogDebug("PACS C-ECHO — no destinations configured yet, retrying in 30s");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                continue;
            }

            logger.LogInformation(
                "PACS C-ECHO checking {Count} destination(s) — next check in {Interval}s",
                opts.Destinations.Length, opts.IntervalSeconds);

            await RunAllChecksAsync(opts, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(opts.IntervalSeconds), stoppingToken);
        }
    }

    private async Task RunAllChecksAsync(PacsCEchoOptions opts, CancellationToken ct)
    {
        foreach (var destination in opts.Destinations)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var success = await pacsSender.VerifyConnectionAsync(destination, ct);
                sw.Stop();

                var result = new PacsCEchoResultDto
                {
                    DestinationAeTitle = destination.AeTitle,
                    Host = destination.Host,
                    Port = destination.Port,
                    Success = success,
                    CheckedAtUtc = DateTime.UtcNow,
                    LatencyMs = sw.Elapsed.TotalMilliseconds
                };

                _results[destination.AeTitle] = result;

                if (success)
                    logger.LogDebug(
                        "C-ECHO to {AeTitle}@{Host}:{Port} succeeded in {Latency:N1}ms",
                        destination.AeTitle, destination.Host, destination.Port, sw.Elapsed.TotalMilliseconds);
                else
                    logger.LogWarning(
                        "C-ECHO to {AeTitle}@{Host}:{Port} failed (no DICOM success status)",
                        destination.AeTitle, destination.Host, destination.Port);
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
                    Host = destination.Host,
                    Port = destination.Port,
                    Success = false,
                    CheckedAtUtc = DateTime.UtcNow,
                    Error = ex.Message
                };

                logger.LogWarning(ex,
                    "C-ECHO check failed for {AeTitle}@{Host}:{Port}",
                    destination.AeTitle, destination.Host, destination.Port);
            }
        }
    }
}
