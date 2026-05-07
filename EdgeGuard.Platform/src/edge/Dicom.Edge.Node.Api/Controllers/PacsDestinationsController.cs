using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Node.Persistence.Entities;
using Dicom.Edge.Node.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Receives PACS destination pushes from the Hub.
/// Separate from <see cref="ConfigurationController"/> so PACS changes
/// are applied immediately and independently of the general settings sync.
/// </summary>
[ApiController]
[Route("api/pacs-destinations")]
public sealed class PacsDestinationsController(
    INodePacsServerRepository pacsRepository,
    ILogger<PacsDestinationsController> logger) : ControllerBase
{
    /// <summary>
    /// POST /api/pacs-destinations/sync
    /// Receives the full list of active PACS assignments from the Hub and performs
    /// a full-replace upsert on <c>node_pacs_servers</c>.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        [FromBody] PacsDestinationsSyncRequest payload,
        CancellationToken ct)
    {
        logger.LogInformation(
            "PACS destinations sync received from Hub: NodeId={NodeId}, Destinations={Count}",
            payload.NodeId, payload.Destinations.Count);

        try
        {
            var servers = payload.Destinations
                .Select(d => new NodePacsServer
                {
                    Id        = d.Id,
                    Name      = d.Name,
                    AeTitle   = d.AeTitle,
                    Host      = d.Host,
                    Port      = d.Port,
                    Priority  = d.Priority,
                    IsEnabled = true,
                    SyncedAt  = payload.SyncedAtUtc,
                })
                .ToList();

            var before = await pacsRepository.GetAllAsync(ct);
            await pacsRepository.SyncFromHubAsync(servers, ct);
            var after  = await pacsRepository.GetAllAsync(ct);

            var upserted = servers.Count;
            var removed  = before.Count(b => !servers.Any(s => s.Id == b.Id));

            logger.LogInformation(
                "PACS sync applied: {Upserted} upserted, {Removed} removed. " +
                "Active destinations: {Active}",
                upserted, removed,
                string.Join(", ", after.Select(p => $"{p.AeTitle}@{p.Host}:{p.Port}")));

            return Ok(new PacsDestinationsSyncResponse
            {
                Accepted      = true,
                UpsertedCount = upserted,
                RemovedCount  = removed,
                AppliedAtUtc  = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to apply PACS destinations sync from Hub for node {NodeId}", payload.NodeId);

            return StatusCode(500, new PacsDestinationsSyncResponse
            {
                Accepted     = false,
                AppliedAtUtc = DateTime.UtcNow,
                Error        = ex.Message,
            });
        }
    }

    /// <summary>
    /// GET /api/pacs-destinations — Returns the current PACS servers table.
    /// Useful for diagnostics.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var servers = await pacsRepository.GetAllAsync(ct);
        return Ok(servers.Select(p => new
        {
            p.Id,
            p.Name,
            p.AeTitle,
            p.Host,
            p.Port,
            p.Priority,
            p.IsEnabled,
            p.SyncedAt,
        }));
    }
}
