using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Node.Persistence.Configuration;
using Dicom.Edge.Node.Persistence.Constants;
using Dicom.Edge.Node.Persistence.Entities;
using Dicom.Edge.Node.Persistence.Repositories;
using Dicom.Edge.Node.Persistence.Services;
using Dicom.Edge.Node.Sender;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Receives PACS destination pushes from the Hub.
/// </summary>
[ApiController]
[Route("api/pacs-destinations")]
public sealed class PacsDestinationsController(
    INodePacsServerRepository pacsRepository,
    INodeSettingsService settingsService,
    INodeConfigurationReloader configReloader,
    IPacsSender pacsSender,
    RoutingRuleLoaderService ruleLoader,
    IPacsBackfillTrigger backfillTrigger,
    ILogger<PacsDestinationsController> logger) : ControllerBase
{
    /// <summary>POST /api/pacs-destinations/sync</summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] PacsDestinationsSyncRequest payload, CancellationToken ct)
    {
        logger.LogInformation("PACS destinations sync received: NodeId={NodeId}, Count={Count}", payload.NodeId, payload.Destinations.Count);
        try
        {
            var servers = payload.Destinations.Select(d => new NodePacsServer
            {
                Id = d.Id, Name = d.Name, AeTitle = d.AeTitle,
                Host = d.Host, Port = d.Port, Priority = d.Priority,
                IsEnabled = true, SyncedAt = payload.SyncedAtUtc,
            }).ToList();

            var before = await pacsRepository.GetAllAsync(ct);
            await pacsRepository.SyncFromHubAsync(servers, ct);
            var after = await pacsRepository.GetAllAsync(ct);

            var upserted = servers.Count;
            var removed  = before.Count(b => !servers.Any(s => s.Id == b.Id));

            await SyncCEchoDestinationsAsync(after, ct);

            // The router caches its destination list and only refreshes every couple of minutes.
            // Until it does, a resend aimed at a just-assigned PACS resolves zero destinations —
            // so reload now, while the sync is still in hand.
            await ruleLoader.LoadNowAsync(ct);

            logger.LogInformation("PACS sync applied: {Upserted} upserted, {Removed} removed. Active: {Active}",
                upserted, removed, string.Join(", ", after.Select(p => $"{p.AeTitle}@{p.Host}:{p.Port}")));

            // A PACS this node had never seen holds none of the historical studies. Replay the
            // ones that match the routing rules; the scan runs in the background so this endpoint
            // still answers the Hub immediately.
            var knownIds = before.Select(b => b.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newPacsIds = after
                .Where(a => a.IsEnabled && !knownIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToList();

            if (newPacsIds.Count > 0)
            {
                logger.LogInformation(
                    "PACS sync introduced {Count} new destination(s) — requesting historical backfill: [{PacsIds}]",
                    newPacsIds.Count, string.Join(", ", newPacsIds));
                backfillTrigger.RequestBackfill(newPacsIds);
            }

            return Ok(new PacsDestinationsSyncResponse { Accepted = true, UpsertedCount = upserted, RemovedCount = removed, AppliedAtUtc = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply PACS destinations sync for node {NodeId}", payload.NodeId);
            return StatusCode(500, new PacsDestinationsSyncResponse { Accepted = false, AppliedAtUtc = DateTime.UtcNow, Error = ex.Message });
        }
    }

    /// <summary>POST /api/pacs-destinations/{aeTitle}/echo — Immediate C-ECHO.</summary>
    [HttpPost("{aeTitle}/echo")]
    public async Task<IActionResult> Echo(string aeTitle, CancellationToken ct)
    {
        var servers = await pacsRepository.GetAllAsync(ct);
        var server  = servers.FirstOrDefault(s => string.Equals(s.AeTitle, aeTitle, StringComparison.OrdinalIgnoreCase) && s.IsEnabled);
        if (server is null)
            return NotFound(new { Error = $"PACS destination '{aeTitle}' not found or disabled." });

        var destination = new PacsDestination { Id = server.Id, AeTitle = server.AeTitle, Host = server.Host, Port = server.Port, UseTls = false };
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var success = await pacsSender.VerifyConnectionAsync(destination, ct);
            sw.Stop();

            logger.LogInformation("On-demand C-ECHO to {AeTitle}@{Host}:{Port} — Success={Success} Latency={Latency:N1}ms",
                destination.AeTitle, destination.Host, destination.Port, success, sw.Elapsed.TotalMilliseconds);

            return Ok(new { destination.AeTitle, destination.Host, destination.Port, Success = success, LatencyMs = sw.Elapsed.TotalMilliseconds, CheckedAtUtc = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "On-demand C-ECHO failed for {AeTitle}@{Host}:{Port}", destination.AeTitle, destination.Host, destination.Port);
            return Ok(new { destination.AeTitle, destination.Host, destination.Port, Success = false, LatencyMs = 0.0, CheckedAtUtc = DateTime.UtcNow, Error = ex.Message });
        }
    }

    /// <summary>GET /api/pacs-destinations</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var servers = await pacsRepository.GetAllAsync(ct);
        return Ok(servers.Select(p => new { p.Id, p.Name, p.AeTitle, p.Host, p.Port, p.Priority, p.IsEnabled, p.SyncedAt }));
    }

    private async Task SyncCEchoDestinationsAsync(IReadOnlyList<NodePacsServer> servers, CancellationToken ct)
    {
        try
        {
            var destinations = servers.Where(s => s.IsEnabled)
                .Select(s => new { s.Id, s.AeTitle, s.Host, s.Port, UseTls = false }).ToArray();
            var json = JsonSerializer.Serialize(destinations);
            await settingsService.SetAsync(NodeSettingKeys.PacsCEcho.Destinations, json, ct);
            configReloader.Reload();
            logger.LogInformation("pacs.cecho.destinations updated with {Count} destination(s)", destinations.Length);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update pacs.cecho.destinations in node_settings");
        }
    }
}
