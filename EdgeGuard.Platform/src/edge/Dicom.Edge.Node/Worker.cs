namespace Dicom.Edge.Node;

/// <summary>
/// Primary Node orchestrator. Logs lifecycle events and keeps the host alive.
/// Actual work is distributed across dedicated hosted services:
/// <list type="bullet">
///   <item><c>HubConfigSyncHostedService</c> — periodic config pull from Hub</item>
///   <item><c>DicomServerHostedService</c> — C-STORE SCP listener</item>
///   <item><c>StudyProcessingHostedService</c> — dequeues and processes studies</item>
/// </list>
/// </summary>
public sealed class Worker(
    ILogger<Worker> logger,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Edge Node worker started on {Machine} at {Time:O}",
            Environment.MachineName, DateTimeOffset.UtcNow);

        lifetime.ApplicationStopping.Register(() =>
            logger.LogInformation("Edge Node worker shutdown requested"));

        try
        {
            // Keep alive — actual processing is handled by dedicated hosted services
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }

        logger.LogInformation("Edge Node worker stopped at {Time:O}", DateTimeOffset.UtcNow);
    }
}
