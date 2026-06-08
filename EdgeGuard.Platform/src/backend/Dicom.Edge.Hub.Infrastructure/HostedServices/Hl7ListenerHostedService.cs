using Dicom.Edge.Hub.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

/// <summary>
/// Hosted service that runs the HL7 Listener as a background service.
///
/// Implements a supervisor loop: if the underlying listener crashes with an
/// unhandled exception, it is restarted with exponential backoff (2s → 60s)
/// rather than tearing down the entire Hub host. Cancellation via stoppingToken
/// is the only path that exits the loop cleanly.
/// </summary>
public class Hl7ListenerHostedService : BackgroundService
{
    private readonly IHl7Listener _listener;
    private readonly ILogger<Hl7ListenerHostedService> _logger;

    private static readonly TimeSpan InitialRestartDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxRestartDelay     = TimeSpan.FromMinutes(1);

    public Hl7ListenerHostedService(
        IHl7Listener listener,
        ILogger<Hl7ListenerHostedService> logger)
    {
        _listener = listener;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HL7 Listener Hosted Service starting (supervised)...");

        var delay = InitialRestartDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _listener.StartAsync(stoppingToken);

                // Normal exit (e.g. cancellation): do not restart.
                if (stoppingToken.IsCancellationRequested)
                    break;

                // Intentional configuration restart (e.g. port change): rebind immediately,
                // no backoff.
                if (_listener.RestartRequested)
                {
                    _logger.LogInformation("HL7 Listener applying configuration restart (rebinding immediately)");
                    delay = InitialRestartDelay;
                    continue;
                }

                // Listener returned without cancellation: treat as unexpected and restart.
                _logger.LogWarning(
                    "HL7 Listener returned unexpectedly without cancellation — restarting in {Delay}s",
                    delay.TotalSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("HL7 Listener Hosted Service cancelled (host shutdown)");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HL7 Listener crashed unexpectedly — restarting in {Delay}s",
                    delay.TotalSeconds);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // Exponential backoff with cap.
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxRestartDelay.TotalSeconds));
        }

        _logger.LogInformation("HL7 Listener Hosted Service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HL7 Listener Hosted Service stopping...");

        try
        {
            await _listener.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while stopping HL7 listener");
        }

        await base.StopAsync(cancellationToken);

        _logger.LogInformation("HL7 Listener Hosted Service stopped");
    }
}
