using Dicom.Edge.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Background service that maintains the connection with the Hub:
/// registration on startup, periodic heartbeats, and configuration pulls.
/// </summary>
public sealed class HubConfigSyncHostedService(
    IHubSyncClient hubClient,
    IServiceScopeFactory scopeFactory,
    IOptions<HubConnectionOptions> options,
    ILogger<HubConfigSyncHostedService> logger) : BackgroundService
{
    private readonly HubConnectionOptions _opts = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opts.Enabled)
        {
            logger.LogInformation("Hub configuration sync is disabled");
            return;
        }

        logger.LogInformation("Hub config sync service started — hub={HubUrl}", _opts.HubBaseUrl);

        if (_opts.RegisterOnStartup)
            await RegisterWithRetryAsync(stoppingToken);

        var heartbeatInterval = TimeSpan.FromSeconds(_opts.HeartbeatIntervalSeconds);
        var configPullInterval = TimeSpan.FromSeconds(_opts.ConfigPullIntervalSeconds);
        var lastConfigPull = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await hubClient.SendHeartbeatAsync(stoppingToken);

                if (DateTime.UtcNow - lastConfigPull > configPullInterval)
                {
                    var config = await hubClient.PullConfigurationAsync(stoppingToken);
                    if (config is { Count: > 0 })
                    {
                        using var scope = scopeFactory.CreateScope();
                        var settingsService = scope.ServiceProvider
                            .GetRequiredService<INodeSettingsService>();
                        await settingsService.ApplyBatchAsync(config, stoppingToken);
                        logger.LogInformation("Applied {Count} config entries from Hub", config.Count);
                    }
                    lastConfigPull = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Hub sync cycle error — retrying next interval");
            }

            await Task.Delay(heartbeatInterval, stoppingToken);
        }

        await hubClient.DeregisterAsync(CancellationToken.None);
    }

    private async Task RegisterWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= _opts.MaxReconnectAttempts; attempt++)
        {
            if (await hubClient.RegisterAsync(ct)) return;

            logger.LogWarning("Registration attempt {Attempt}/{Max} failed",
                attempt, _opts.MaxReconnectAttempts);

            await Task.Delay(
                TimeSpan.FromSeconds(_opts.ReconnectDelaySeconds * attempt), ct);
        }

        logger.LogError("Failed to register with Hub after {Max} attempts", _opts.MaxReconnectAttempts);
    }
}
