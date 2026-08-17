using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Node.Persistence.Context;
using Dicom.Edge.Node.Persistence.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EquipmentEntity = Dicom.Edge.Models.Equipment.Equipment;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Receives equipment-catalog pushes from the Hub and applies them to the node's local
/// SQLite database (full replace), then forces an immediate equipment-catalog reload so
/// association validation and MWL filtering pick up the change without delay.
/// </summary>
[ApiController]
[Route("api/equipment")]
public sealed class EquipmentController(
    IDbContextFactory<EdgeNodeDbContext> dbFactory,
    EquipmentLoaderService equipmentLoader,
    ILogger<EquipmentController> logger) : ControllerBase
{
    /// <summary>POST /api/equipment/sync — Hub pushes the full equipment set for this node.</summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        [FromBody] EquipmentSyncRequest payload, CancellationToken ct)
    {
        logger.LogInformation(
            "Equipment sync received: NodeId={NodeId}, Count={Count}",
            payload.NodeId, payload.Equipment.Count);

        try
        {
            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var existing = await ctx.Equipment
                .Include(e => e.Modalities)
                .ToListAsync(ct);

            // Full replace: remove equipment not present in the incoming set.
            var incomingIds = payload.Equipment.Select(e => e.Id).ToHashSet();
            var stale = existing.Where(e => !incomingIds.Contains(e.Id)).ToList();
            if (stale.Count > 0)
                ctx.Equipment.RemoveRange(stale);

            foreach (var entry in payload.Equipment)
            {
                var codes = (entry.ModalityCodes ?? [])
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim().ToUpperInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var current = existing.FirstOrDefault(e => e.Id == entry.Id);
                if (current is null)
                {
                    ctx.Equipment.Add(new EquipmentEntity
                    {
                        Id             = entry.Id,
                        AeTitle        = entry.AeTitle,
                        DisplayName    = entry.DisplayName,
                        StationAeTitle = entry.StationAeTitle,
                        StationName    = entry.StationName,
                        IpAddress      = entry.IpAddress,
                        IsEnabled      = entry.IsEnabled,
                        CreatedAt      = DateTime.UtcNow,
                        Modalities     = codes
                            .Select(c => new Dicom.Edge.Models.Equipment.EquipmentModality
                            {
                                EquipmentId  = entry.Id,
                                ModalityCode = c,
                            })
                            .ToList(),
                    });
                }
                else
                {
                    current.AeTitle        = entry.AeTitle;
                    current.DisplayName    = entry.DisplayName;
                    current.StationAeTitle = entry.StationAeTitle;
                    current.StationName    = entry.StationName;
                    current.IpAddress      = entry.IpAddress;
                    current.IsEnabled      = entry.IsEnabled;
                    current.UpdatedAt      = DateTime.UtcNow;

                    // Replace the modality set (EF removes orphans, inserts new rows).
                    current.Modalities.Clear();
                    foreach (var c in codes)
                        current.Modalities.Add(new Dicom.Edge.Models.Equipment.EquipmentModality
                        {
                            EquipmentId  = current.Id,
                            ModalityCode = c,
                        });
                }
            }

            await ctx.SaveChangesAsync(ct);

            // Reload the in-memory catalog immediately so the SCP sees the change now.
            await equipmentLoader.LoadNowAsync(ct);

            logger.LogInformation(
                "Equipment applied: {Applied} upserted, {Removed} removed for node {NodeId}",
                payload.Equipment.Count, stale.Count, payload.NodeId);

            return Ok(new EquipmentSyncResponse
            {
                Accepted     = true,
                AppliedCount = payload.Equipment.Count,
                RemovedCount = stale.Count,
                AppliedAt    = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply equipment sync for node {NodeId}", payload.NodeId);

            return StatusCode(500, new EquipmentSyncResponse
            {
                Accepted  = false,
                AppliedAt = DateTime.UtcNow,
                Error     = ex.Message,
            });
        }
    }
}
