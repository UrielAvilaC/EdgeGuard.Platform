using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Background service that maintains the connection with the Hub:
/// registration on startup, periodic heartbeats, and configuration pulls.
/// On first registration, persists the API key to the node_settings SQLite table.
/// On subsequent startups, loads the API key from the database.
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

        // Sync appsettings → DB for any identity field that is still at its seed default (empty)
        await SyncAppsettingsToDatabase(stoppingToken);

        // Load API key from DB if it exists (restart scenario)
        var hasExistingKey = await LoadApiKeyFromDatabaseAsync(stoppingToken);

        if (!hasExistingKey && _opts.RegisterOnStartup)
        {
            // First time — no API key in DB, must register to obtain one
            await RegisterWithRetryAsync(stoppingToken);
        }
        else if (hasExistingKey)
        {
            logger.LogInformation("API key found in database — skipping registration, starting sync loop");
        }

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

    /// <summary>
    /// Resolves Hub identity values using the DB as source of truth.
    /// For any empty DB value, falls back to appsettings and seeds the DB immediately.
    /// </summary>
    private async Task SyncAppsettingsToDatabase(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<INodeSettingsService>();

            _opts.NodeName = await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.NodeName, _opts.NodeName, ct) ?? _opts.NodeName;
            _opts.AeTitle = await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.AeTitle, _opts.AeTitle, ct) ?? _opts.AeTitle;
            _opts.IpAddress = await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.IpAddress, _opts.IpAddress, ct) ?? _opts.IpAddress;
            _opts.Version = await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.Version, _opts.Version, ct);
            _opts.Location = await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.Location, _opts.Location, ct);
            _opts.FacilityName = await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.FacilityName, _opts.FacilityName, ct);
            _opts.ApiEndpoint = await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.ApiEndpoint, _opts.ApiEndpoint, ct);

            logger.LogDebug("DB-first identity sync complete");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DB-first identity sync failed — continuing with current option values");
        }
    }

    private static async Task<string?> SyncIfEmptyAsync(
        INodeSettingsService settings, string key, string? appsettingsValue, CancellationToken ct)
    {
        var existing = await settings.GetAsync<string>(key, string.Empty, ct);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        if (!string.IsNullOrWhiteSpace(appsettingsValue))
            await settings.SetAsync(key, appsettingsValue, ct);

        return appsettingsValue;
    }

    private async Task<bool> LoadApiKeyFromDatabaseAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<INodeSettingsService>();
            var apiKey = await settingsService.GetAsync<string>(
                SharedNodeSettingKeys.Hub.ApiKey, string.Empty, ct);

            if (!string.IsNullOrEmpty(apiKey))
            {
                if (hubClient is HubSyncClient concrete)
                    concrete.SetApiKey(apiKey);

                logger.LogInformation("API key loaded from database for Hub authentication");
                return true;
            }

            logger.LogDebug("No API key found in database — will register for a new one");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load API key from database");
            return false;
        }
    }

    private async Task RegisterWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= _opts.MaxReconnectAttempts; attempt++)
        {
            var result = await hubClient.RegisterAsync(ct);

            if (result.Success)
            {
                if (!string.IsNullOrEmpty(result.NewApiKey))
                {
                    // First registration — persist the new API key to SQLite
                    await PersistApiKeyAsync(result.NewApiKey, ct);
                }
                else
                {
                    // Re-registration — API key already in SQLite, nothing to persist
                    logger.LogInformation(
                        "Re-registered with Hub (NodeId={NodeId})", result.NodeId);
                }
                return;
            }

            logger.LogWarning("Registration attempt {Attempt}/{Max} failed",
                attempt, _opts.MaxReconnectAttempts);

            await Task.Delay(
                TimeSpan.FromSeconds(_opts.ReconnectDelaySeconds * attempt), ct);
        }

        logger.LogError("Failed to register with Hub after {Max} attempts", _opts.MaxReconnectAttempts);
    }

    private async Task PersistApiKeyAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<INodeSettingsService>();
            await settingsService.SetAsync(SharedNodeSettingKeys.Hub.ApiKey, apiKey, ct);
            logger.LogInformation("API key persisted to database — it will survive restarts");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Failed to persist API key to database. " +
                "The node will need to be re-registered manually.");
        }
    }
}
