using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Models.Dicom;
using Dicom.Edge.Models.Enums;
using Dicom.Edge.Models.Metrics;
using Dicom.Edge.Node.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Background service that maintains the connection with the Hub:
/// registration on startup, one-time pull on startup, periodic heartbeats,
/// and pull fallback every configPullInterval (in case a push was missed).
/// Config is primarily delivered via Hub → Node push (POST /api/configuration/apply).
/// The periodic pull is a safety net only.
/// </summary>
public sealed class HubConfigSyncHostedService(
    IHubSyncClient hubClient,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<HubConnectionOptions> optionsMonitor,
    ILogger<HubConfigSyncHostedService> logger) : BackgroundService
{
    private HubConnectionOptions Opts => optionsMonitor.CurrentValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Opts.Enabled)
        {
            logger.LogInformation("Hub configuration sync is disabled");
            return;
        }

        logger.LogInformation("Hub config sync service started — hub={HubUrl}", Opts.HubBaseUrl);

        // Sync appsettings → DB for any identity field that is still at its seed default (empty)
        await SyncAppsettingsToDatabase(stoppingToken);

        // Load API key from DB if it exists (restart scenario)
        var hasExistingKey = await LoadApiKeyFromDatabaseAsync(stoppingToken);

        if (!hasExistingKey && Opts.RegisterOnStartup)
        {
            // First time — no API key in DB, must register to obtain one
            await RegisterWithRetryAsync(stoppingToken);
        }
        else if (hasExistingKey)
        {
            logger.LogInformation("API key found in database — skipping registration, starting sync loop");
        }

        // ── Pull once on startup to apply any Hub changes made while node was offline ──
        await PullAndApplyConfigAsync(stoppingToken);

        var lastConfigPull = DateTime.UtcNow;
        var lastTelemetry  = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var heartbeatInterval = TimeSpan.FromSeconds(Opts.HeartbeatIntervalSeconds);
                await Task.Delay(heartbeatInterval, stoppingToken);

                await hubClient.SendHeartbeatAsync(stoppingToken);

                // Fallback pull — safety net in case a Hub push was missed
                var configPullInterval = TimeSpan.FromSeconds(Opts.ConfigPullIntervalSeconds);
                if (DateTime.UtcNow - lastConfigPull > configPullInterval)
                {
                    logger.LogDebug("Fallback config pull triggered (interval={Interval}s)", configPullInterval.TotalSeconds);
                    await PullAndApplyConfigAsync(stoppingToken);
                    lastConfigPull = DateTime.UtcNow;
                }

                // Telemetry push — same cadence as config pull
                if (DateTime.UtcNow - lastTelemetry > configPullInterval)
                {
                    await PushTelemetryAsync(lastTelemetry, stoppingToken);
                    lastTelemetry = DateTime.UtcNow;
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
        }

        await hubClient.DeregisterAsync(CancellationToken.None);
    }

    private async Task PullAndApplyConfigAsync(CancellationToken ct)
    {
        try
        {
            var config = await hubClient.PullConfigurationAsync(ct);
            if (config is { Count: > 0 })
            {
                using var scope = scopeFactory.CreateScope();
                var settingsService = scope.ServiceProvider.GetRequiredService<INodeSettingsService>();
                await settingsService.ApplyBatchAsync(config, ct);
                logger.LogInformation("Applied {Count} config entries from Hub pull", config.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Config pull from Hub failed — will retry on next cycle");
        }
    }

    /// <summary>
    /// Writes appsettings values to the DB for any Hub identity field that is still empty.
    /// This ensures the DB reflects the local configuration after a fresh install,
    /// while never overwriting values that were already set (e.g. by the Hub UI).
    /// </summary>
    private async Task SyncAppsettingsToDatabase(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<INodeSettingsService>();

            await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.NodeName,  Opts.NodeName,    ct);
            await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.AeTitle,   Opts.AeTitle,     ct);
            await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.IpAddress, Opts.IpAddress,   ct);
            await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.Version,   Opts.Version,     ct);
            await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.Location,  Opts.Location,    ct);
            await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.FacilityName, Opts.FacilityName, ct);
            await SyncIfEmptyAsync(settings, SharedNodeSettingKeys.General.ApiEndpoint,  Opts.ApiEndpoint,  ct);

            logger.LogDebug("Appsettings → DB identity sync complete");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Appsettings → DB sync failed — continuing with appsettings values");
        }
    }

    private static async Task SyncIfEmptyAsync(
        INodeSettingsService settings, string key, string? value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var existing = await settings.GetAsync<string>(key, string.Empty, ct);
        if (string.IsNullOrEmpty(existing))
            await settings.SetAsync(key, value, ct);
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
                {
                    concrete.SetApiKey(apiKey);

                    var nodeId = await settingsService.GetAsync<string>(
                        SharedNodeSettingKeys.Hub.NodeId, string.Empty, ct);
                    if (!string.IsNullOrEmpty(nodeId))
                    {
                        concrete.SetRegisteredNodeId(nodeId);
                        logger.LogInformation("NodeId restored from database — NodeId={NodeId}", nodeId);
                    }
                    else
                    {
                        logger.LogWarning("API key found in database but NodeId is missing — node will re-register to obtain a NodeId");
                        return false;
                    }
                }

                logger.LogInformation("API key and NodeId loaded from database for Hub authentication");
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
        for (var attempt = 1; attempt <= Opts.MaxReconnectAttempts; attempt++)
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

                // Always persist/update NodeId so it survives restarts
                if (!string.IsNullOrEmpty(result.NodeId))
                    await PersistNodeIdAsync(result.NodeId, ct);

                return;
            }

            logger.LogWarning("Registration attempt {Attempt}/{Max} failed",
                attempt, Opts.MaxReconnectAttempts);

            await Task.Delay(
                TimeSpan.FromSeconds(Opts.ReconnectDelaySeconds * attempt), ct);
        }

        logger.LogError("Failed to register with Hub after {Max} attempts", Opts.MaxReconnectAttempts);
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

    private async Task PersistNodeIdAsync(string nodeId, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<INodeSettingsService>();
            await settingsService.SetAsync(SharedNodeSettingKeys.Hub.NodeId, nodeId, ct);
            logger.LogInformation("NodeId persisted to database — NodeId={NodeId}", nodeId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Failed to persist NodeId to database — NodeId={NodeId}", nodeId);
        }
    }

    // ── Telemetry ─────────────────────────────────────────────────────────────

    private async Task PushTelemetryAsync(DateTime periodStart, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(hubClient.RegisteredNodeId)) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EdgeNodeDbContext>>()
                          .CreateDbContext();

            var periodEnd = DateTime.UtcNow;

            // ── Association aggregates ────────────────────────────────────
            var assocs = await ctx.Associations
                .AsNoTracking()
                .Where(a => a.ConnectedAt >= periodStart && a.ConnectedAt <= periodEnd)
                .ToListAsync(ct);

            // ── Study-metrics aggregates ──────────────────────────────────
            var metrics = await ctx.Metrics
                .AsNoTracking()
                .Where(m => m.FirstImageAt >= periodStart && m.FirstImageAt <= periodEnd)
                .ToListAsync(ct);

            var avgDuration = metrics.Count > 0
                ? metrics.Average(m => m.ReceptionDuration.TotalMilliseconds)
                : (double?)null;

            var avgThroughput = metrics.Count > 0
                ? metrics.Average(m => m.ReceptionThroughputMbps)
                : (double?)null;

            var request = new NodeTelemetryRequest
            {
                NodeId                    = hubClient.RegisteredNodeId,
                PeriodStart               = periodStart,
                PeriodEnd                 = periodEnd,
                TotalAssociations         = assocs.Count,
                AcceptedAssociations      = assocs.Count(a => a.Status == AssociationStatus.Completed),
                RejectedAssociations      = assocs.Count(a => a.Status == AssociationStatus.Rejected),
                AbortedAssociations       = assocs.Count(a => a.Status == AssociationStatus.Aborted),
                TotalImagesReceived       = assocs.Sum(a => a.ImagesReceived),
                CompletedStudies          = metrics.Count,
                TotalBytesReceived        = metrics.Sum(m => m.TotalSizeBytes),
                AverageReceptionDurationMs = avgDuration,
                AverageThroughputMbps     = avgThroughput,
            };

            await hubClient.SendTelemetryAsync(request, ct);
            logger.LogDebug(
                "Telemetry pushed — Associations={Total} Completed={Studies} Period={Start:O}→{End:O}",
                request.TotalAssociations, request.CompletedStudies, periodStart, periodEnd);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telemetry push failed — will retry next cycle");
        }
    }
}
