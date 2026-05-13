using Dicom.Edge.Contracts.Node;
using Dicom.Edge.Contracts.Worklist;
using Dicom.Edge.Node.Worklist;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Provides DICOM/worklist status information for monitoring.
/// </summary>
[ApiController]
[Route("api/dicom")]
public sealed class DicomStatusController(
    IWorklistManager worklistManager,
    ILogger<DicomStatusController> logger) : ControllerBase
{
    /// <summary>
    /// GET /api/dicom/worklist/count — Returns the active worklist item count.
    /// </summary>
    [HttpGet("worklist/count")]
    public async Task<IActionResult> GetWorklistCount(CancellationToken ct)
    {
        var count = await worklistManager.GetActiveCountAsync(ct);

        logger.LogDebug("Worklist active count requested: {Count}", count);

        return Ok(new WorklistCountResponse
        {
            ActiveItems = count,
            TimestampUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// GET /api/dicom/worklist — Returns active worklist items as DTOs.
    /// </summary>
    [HttpGet("worklist")]
    public async Task<IActionResult> GetWorklistItems(CancellationToken ct)
    {
        var items = await worklistManager.GetActiveItemsAsync(ct);

        var dtos = items.Select(i => new WorklistItemDto
        {
            AccessionNumber = i.AccessionNumber ?? string.Empty,
            PatientId = i.PatientId ?? string.Empty,
            PatientName = i.PatientName ?? string.Empty,
            ScheduledDate = i.ReceivedAt,
            Modality = i.Modality ?? string.Empty,
            ProcedureDescription = i.MessageType
        }).ToList();

        return Ok(new WorklistQueryResponse { Items = dtos });
    }
}
