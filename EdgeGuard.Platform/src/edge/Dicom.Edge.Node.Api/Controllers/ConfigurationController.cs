using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Models.Core;
using Dicom.Edge.Models.Routing;
using Dicom.Edge.Node.Persistence.Constants;
using Dicom.Edge.Node.Persistence.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Receives configuration pushes from the Hub and reports the current config version.
/// </summary>
[ApiController]
[Route("api")]
public sealed class ConfigurationController(
    INodeSettingsService settingsService,
    IDbContextFactory<EdgeNodeDbContext> dbFactory,
    ILogger<ConfigurationController> logger) : ControllerBase
{
    private const string ConfigSourceHub = "Hub";
    private const string ConfigVersionDefault = "not-initialized";

    /// <summary>
    /// POST /api/configuration/apply — Receives a full configuration snapshot from the Hub.
    /// </summary>
    [HttpPost("configuration/apply")]
    public async Task<IActionResult> Apply([FromBody] NodeConfigSyncDto payload, CancellationToken ct)
    {
        if (payload.Settings is null || payload.Settings.Count == 0)
        {
            return BadRequest(new ConfigSyncResultDto
            {
                Accepted = false,
                Error = "No settings provided in the payload.",
                AppliedAtUtc = DateTime.UtcNow
            });
        }

        try
        {
            logger.LogInformation(
                "Receiving config push from Hub. NodeId={NodeId}, Version={Version}, SettingCount={Count}",
                payload.NodeId, payload.ConfigVersion, payload.Settings.Count);

            await settingsService.ApplyBatchAsync(payload.Settings, ct);

            // Update system metadata keys
            await settingsService.SetAsync(
                NodeSettingKeys.System.ConfigVersion, payload.ConfigVersion, ct);
            await settingsService.SetAsync(
                NodeSettingKeys.System.LastConfigAppliedUtc, DateTime.UtcNow.ToString("O"), ct);
            await settingsService.SetAsync(
                NodeSettingKeys.System.LastConfigSource, ConfigSourceHub, ct);

            await settingsService.ReloadAsync(ct);

            logger.LogInformation(
                "Configuration applied successfully. Version={Version}, Applied={Count} settings",
                payload.ConfigVersion, payload.Settings.Count);

            // PACS destinations are now pushed separately via POST /api/pacs-destinations/sync

            return Ok(new ConfigSyncResultDto
            {
                Accepted       = true,
                AppliedVersion = payload.ConfigVersion,
                UpdatedCount   = payload.Settings.Count,
                AppliedAtUtc   = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply config push. Version={Version}", payload.ConfigVersion);

            return StatusCode(500, new ConfigSyncResultDto
            {
                Accepted = false,
                Error = ex.Message,
                AppliedAtUtc = DateTime.UtcNow
            });
        }
    }

    private async Task ApplyPacsRoutingRulesAsync(
        IReadOnlyList<PacsDestinationSyncEntry> destinations,
        string nodeId,
        CancellationToken ct)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);

        // Remove routing rules for PACS IDs no longer assigned
        var incomingIds = destinations.Select(d => d.Id).ToHashSet();
        var stale = await ctx.RoutingRules
            .Where(r => !incomingIds.Contains(r.Id))
            .ToListAsync(ct);
        if (stale.Count > 0) ctx.RoutingRules.RemoveRange(stale);

        // Upsert one rule per PACS destination (accept-all → specific PACS)
        foreach (var dest in destinations)
        {
            var existing = await ctx.RoutingRules.FindAsync([dest.Id], ct);
            if (existing is null)
            {
                ctx.RoutingRules.Add(new RoutingRule
                {
                    Id                 = dest.Id,
                    Name               = $"Hub-PACS-{dest.AeTitle}",
                    Priority           = dest.Priority,
                    IsEnabled          = true,
                    DestinationAeTitle = dest.AeTitle,
                    SendToHub          = false,
                });
            }
            else
            {
                existing.DestinationAeTitle = dest.AeTitle;
                existing.Priority           = dest.Priority;
                existing.IsEnabled          = true;
            }
        }

        await ctx.SaveChangesAsync(ct);
    }

    /// <summary>
    /// GET /api/configuration/version — Returns the node's current config version hash.
    /// </summary>
    [HttpGet("configuration/version")]
    public async Task<IActionResult> GetVersion(CancellationToken ct)
    {
        var version = await settingsService.GetAsync<string>(
            NodeSettingKeys.System.ConfigVersion, ConfigVersionDefault, ct);

        return Ok(new { configVersion = version });
    }
}
