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
/// </summary>
public sealed class PacsCEchoHostedService(
    IPacsSender pacsSender,
    IOptions<PacsCEchoOptions> options,
    ILogger<PacsCEchoHostedService> logger) : BackgroundService, IPacsCEchoMonitor
{
    private readonly PacsCEchoOptions _opts = options.Value;
    private readonly ConcurrentDictionary<string, PacsCEchoResultDto> _results = new();

    public IReadOnlyList<PacsCEchoResultDto> GetLatestResults() =>
        _results.Values.ToList().AsReadOnly();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opts.Enabled || _opts.Destinations.Length == 0)
        {
            logger.LogInformation("PACS C-ECHO monitor is disabled or has no destinations configured");
            return;
        }

        logger.LogInformation(
            "PACS C-ECHO monitor started — checking {Count} destination(s) every {Interval}s",
            _opts.Destinations.Length, _opts.IntervalSeconds);

        var interval = TimeSpan.FromSeconds(_opts.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAllChecksAsync(stoppingToken);
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunAllChecksAsync(CancellationToken ct)
    {
        foreach (var destination in _opts.Destinations)
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
                {
                    logger.LogDebug(
                        "C-ECHO to {AeTitle}@{Host}:{Port} succeeded in {Latency:N1}ms",
                        destination.AeTitle, destination.Host, destination.Port, sw.Elapsed.TotalMilliseconds);
                }
                else
                {
                    logger.LogWarning(
                        "C-ECHO to {AeTitle}@{Host}:{Port} failed (no DICOM success status)",
                        destination.AeTitle, destination.Host, destination.Port);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                var errorResult = new PacsCEchoResultDto
                {
                    DestinationAeTitle = destination.AeTitle,
                    Host = destination.Host,
                    Port = destination.Port,
                    Success = false,
                    CheckedAtUtc = DateTime.UtcNow,
                    Error = ex.Message
                };

                _results[destination.AeTitle] = errorResult;

                logger.LogWarning(ex,
                    "C-ECHO check failed for {AeTitle}@{Host}:{Port}",
                    destination.AeTitle, destination.Host, destination.Port);
            }
        }
    }
}
