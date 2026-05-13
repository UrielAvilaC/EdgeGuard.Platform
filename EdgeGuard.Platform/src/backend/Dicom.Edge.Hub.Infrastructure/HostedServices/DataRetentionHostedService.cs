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
    IOptions<HubBackgroundJobsOptions> options,
    ILogger<DataRetentionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnableDataRetention)
        {
            logger.LogInformation("Data retention service is disabled via configuration");
            return;
        }

        logger.LogInformation(
            "Data retention service started — interval={Interval}s",
            options.Value.DataRetentionIntervalSeconds);

        // Delay first cycle to let startup I/O settle
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
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

            await Task.Delay(
                TimeSpan.FromSeconds(options.Value.DataRetentionIntervalSeconds),
                stoppingToken);
        }
    }
}
