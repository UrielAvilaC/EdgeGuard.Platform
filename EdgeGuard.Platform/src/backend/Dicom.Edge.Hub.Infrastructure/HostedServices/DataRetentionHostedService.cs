using Dicom.Edge.Hub.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

/// <summary>
/// Background service that periodically executes the enterprise data retention policy.
/// Uses <see cref="IServiceScopeFactory"/> for proper scoped DI in a singleton BackgroundService.
/// </summary>
public sealed class DataRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<HubBackgroundJobsOptions> options,
    ILogger<DataRetentionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Data retention service started");

        // Delay first cycle to let startup I/O settle
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Re-read each cycle so enable/disable and interval changes apply without a restart.
            var opts = options.CurrentValue;

            if (opts.EnableDataRetention)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var retentionService = scope.ServiceProvider
                        .GetRequiredService<IHubDataRetentionService>();

                    await retentionService.ExecuteRetentionAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Expected on shutdown
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled error in data retention cycle — retrying next interval");
                }
            }

            await Task.Delay(
                TimeSpan.FromSeconds(opts.DataRetentionIntervalSeconds),
                stoppingToken);
        }
    }
}
