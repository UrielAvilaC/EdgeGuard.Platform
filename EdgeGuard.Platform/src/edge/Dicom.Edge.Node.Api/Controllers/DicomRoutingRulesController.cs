using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Models.Routing;
using Dicom.Edge.Node.Persistence.Context;
using Dicom.Edge.Node.Persistence.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Receives DICOM routing rule pushes from the Hub and applies them to the
/// node's local SQLite database, then forces an immediate router reload.
/// </summary>
[ApiController]
[Route("api/dicom-routing-rules")]
public sealed class DicomRoutingRulesController(
    IDbContextFactory<EdgeNodeDbContext> dbFactory,
    RoutingRuleLoaderService routingRuleLoader,
    ILogger<DicomRoutingRulesController> logger) : ControllerBase
{
    /// <summary>POST /api/dicom-routing-rules/sync — Hub pushes the full rule set.</summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        [FromBody] DicomRoutingRulesSyncRequest payload, CancellationToken ct)
    {
        logger.LogInformation(
            "DICOM routing rules sync received: NodeId={NodeId}, RuleCount={Count}",
            payload.NodeId, payload.Rules.Count);

        try
        {
            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            // Full replace: remove rules not in the incoming set
            var incomingIds = payload.Rules.Select(r => r.Id).ToHashSet();
            var stale = await ctx.RoutingRules
                .Where(r => !incomingIds.Contains(r.Id))
                .ToListAsync(ct);

            if (stale.Count > 0)
                ctx.RoutingRules.RemoveRange(stale);

            // Upsert each incoming rule
            foreach (var entry in payload.Rules)
            {
                var existing = await ctx.RoutingRules.FindAsync([entry.Id], ct);
                if (existing is null)
                {
                    ctx.RoutingRules.Add(new RoutingRule
                    {
                        Id                      = entry.Id,
                        Name                    = entry.Name,
                        Priority                = entry.Priority,
                        IsEnabled               = entry.IsEnabled,
                        Modality                = entry.MatchModality,
                        SourceAeTitle           = entry.MatchSourceAeTitle,
                        InstitutionName         = entry.MatchInstitution,
                        StudyDescriptionContains= entry.MatchStudyDescContains,
                        MinInstanceCount        = entry.MinInstanceCount,
                        MaxInstanceCount        = entry.MaxInstanceCount,
                        DestinationAeTitle      = entry.DestinationAeTitle,
                        SendToPacs              = entry.SendToPacs,
                        SendToHub               = entry.SendToHub,
                        AnonymizeBeforeSending  = entry.AnonymizeBeforeSending,
                        CreatedAt               = DateTime.UtcNow,
                        CreatedBy               = "Hub",
                    });
                }
                else
                {
                    existing.Name                    = entry.Name;
                    existing.Priority                = entry.Priority;
                    existing.IsEnabled               = entry.IsEnabled;
                    existing.Modality                = entry.MatchModality;
                    existing.SourceAeTitle           = entry.MatchSourceAeTitle;
                    existing.InstitutionName         = entry.MatchInstitution;
                    existing.StudyDescriptionContains= entry.MatchStudyDescContains;
                    existing.MinInstanceCount        = entry.MinInstanceCount;
                    existing.MaxInstanceCount        = entry.MaxInstanceCount;
                    existing.SendToPacs              = entry.SendToPacs;
                    existing.SendToHub               = entry.SendToHub;
                    existing.AnonymizeBeforeSending  = entry.AnonymizeBeforeSending;
                    existing.UpdatedAt               = DateTime.UtcNow;
                    existing.UpdatedBy               = "Hub";
                }
            }

            await ctx.SaveChangesAsync(ct);

            // Reload router immediately so new rules take effect without waiting 2 min
            await routingRuleLoader.LoadNowAsync(ct);

            logger.LogInformation(
                "DICOM routing rules applied: {Applied} upserted, {Removed} removed for node {NodeId}",
                payload.Rules.Count, stale.Count, payload.NodeId);

            return Ok(new DicomRoutingRulesSyncResponse
            {
                Accepted     = true,
                AppliedCount = payload.Rules.Count,
                RemovedCount = stale.Count,
                AppliedAt    = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to apply DICOM routing rules sync for node {NodeId}", payload.NodeId);

            return StatusCode(500, new DicomRoutingRulesSyncResponse
            {
                Accepted  = false,
                AppliedAt = DateTime.UtcNow,
                Error     = ex.Message,
            });
        }
    }
}
