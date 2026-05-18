using Dicom.Edge.Hub.Application.Dashboard;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Policies.ViewMetrics)]
[EnableRateLimiting("api")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// GET /api/dashboard/summary — Aggregated KPIs for the dashboard.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await dashboardService.GetSummaryAsync(ct);
        return Ok(summary);
    }
}
