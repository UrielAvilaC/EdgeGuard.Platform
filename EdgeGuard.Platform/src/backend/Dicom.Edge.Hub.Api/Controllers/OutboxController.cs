using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>
/// Monitoring and operator actions for the unified outbox (node-sync + notifications)
/// via the <c>vw_outbox_activity</c> view. Reads require ViewSystemStatus; retry and
/// dead-letter actions require ManageQueue.
/// </summary>
[ApiController]
[Route("api/outbox")]
[Authorize(Policy = Policies.ViewSystemStatus)]
[EnableRateLimiting("api")]
public sealed class OutboxController(
    IOutboxActivityRepository activityRepository,
    IOutboxTopicRepository topicRepository,
    INotificationRepository notificationRepository,
    INodeOutboxRepository nodeOutboxRepository,
    IUnitOfWork unitOfWork) : ControllerBase
{
    /// <summary>Paged, filterable unified outbox activity (most recent first).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<OutboxActivity>>> Get(
        [FromQuery] string? category,
        [FromQuery] string? topic,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 200);

        var (items, total) = await activityRepository.QueryAsync(
            category, topic, status, (page - 1) * pageSize, pageSize, ct);

        return Ok(new PagedResult<OutboxActivity>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    /// <summary>The outbox topic catalog (for filter dropdowns / grouping).</summary>
    [HttpGet("topics")]
    public async Task<ActionResult<IReadOnlyList<OutboxTopic>>> GetTopics(CancellationToken ct) =>
        Ok(await topicRepository.GetAllAsync(ct));

    /// <summary>Requeues a failed/stuck entry for immediate re-dispatch.</summary>
    [HttpPost("{store}/{id}/retry")]
    [Authorize(Policy = Policies.ManageQueue)]
    public Task<IActionResult> Retry(string store, string id, CancellationToken ct) =>
        MutateAsync(store, id, requeue: true, ct);

    /// <summary>Marks an entry as permanently failed (dead-letter).</summary>
    [HttpPost("{store}/{id}/dead-letter")]
    [Authorize(Policy = Policies.ManageQueue)]
    public Task<IActionResult> DeadLetter(string store, string id, CancellationToken ct) =>
        MutateAsync(store, id, requeue: false, ct);

    private async Task<IActionResult> MutateAsync(string store, string id, bool requeue, CancellationToken ct)
    {
        switch (store.ToLowerInvariant())
        {
            case "notifications":
                var n = await notificationRepository.GetByIdAsync(id, ct);
                if (n is null) return NotFound();
                if (requeue) n.Requeue(); else n.MarkFailed("Manually dead-lettered");
                await notificationRepository.UpdateAsync(n, ct);
                break;

            case "node":
                var m = await nodeOutboxRepository.GetByIdAsync(id, ct);
                if (m is null) return NotFound();
                if (requeue) m.Requeue(); else m.MarkFailed("Manually dead-lettered");
                await nodeOutboxRepository.UpdateAsync(m, ct);
                break;

            default:
                return BadRequest($"Unknown store '{store}' (expected 'notifications' or 'node').");
        }

        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }
}
