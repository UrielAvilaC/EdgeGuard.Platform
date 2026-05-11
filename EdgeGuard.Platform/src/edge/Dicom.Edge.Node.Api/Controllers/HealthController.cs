using Dicom.Edge.Contracts.Node;
using Dicom.Edge.Node.Worklist;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Basic health-check endpoint exposed by the Edge Node.
/// Used by the Hub to verify node reachability.
/// </summary>
[ApiController]
[Route("api")]
public sealed class HealthController(
    IWorklistManager worklistManager) : ControllerBase
{
    private const string StatusHealthy = "Healthy";

    /// <summary>
    /// GET /api/health — Returns node health status.
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var activeItems = await worklistManager.GetActiveCountAsync(ct);

        var response = new NodeHealthResponse
        {
            Status = StatusHealthy,
            TimestampUtc = DateTime.UtcNow,
            NodeName = Environment.MachineName,
            ActiveWorklistItems = activeItems,
            DicomServerRunning = true
        };

        return Ok(response);
    }
}
