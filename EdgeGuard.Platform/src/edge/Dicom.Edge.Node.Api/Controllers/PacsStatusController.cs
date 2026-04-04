using Dicom.Edge.Abstractions.Monitoring;
using Dicom.Edge.Contracts.Node;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Exposes PACS C-ECHO connectivity status for monitoring.
/// </summary>
[ApiController]
[Route("api/dicom/pacs")]
public sealed class PacsStatusController(
    IPacsCEchoMonitor cEchoMonitor) : ControllerBase
{
    /// <summary>
    /// GET /api/dicom/pacs/status — Returns C-ECHO results for all PACS destinations.
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetPacsStatus()
    {
        var results = cEchoMonitor.GetLatestResults();

        return Ok(new PacsCEchoStatusResponse
        {
            Destinations = results,
            TimestampUtc = DateTime.UtcNow
        });
    }
}
