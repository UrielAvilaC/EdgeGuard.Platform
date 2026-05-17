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
    /// Supports optional query parameters: modality, patientId, accessionNumber, from, to.
    /// </summary>
    [HttpGet("worklist")]
    public async Task<IActionResult> GetWorklistItems(
        [FromQuery] string? modality,
        [FromQuery] string? patientId,
        [FromQuery] string? accessionNumber,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var hasFilter = !string.IsNullOrEmpty(modality)
            || !string.IsNullOrEmpty(patientId)
            || !string.IsNullOrEmpty(accessionNumber)
            || from.HasValue
            || to.HasValue;

        IReadOnlyList<Dicom.Edge.Node.Worklist.WorklistItem> items;

        if (hasFilter)
        {
            items = await worklistManager.QueryAsync(from, to, modality, ct);

            if (!string.IsNullOrEmpty(patientId))
                items = items.Where(i => string.Equals(i.PatientId, patientId, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(accessionNumber))
                items = items.Where(i => string.Equals(i.AccessionNumber, accessionNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else
        {
            items = await worklistManager.GetActiveItemsAsync(ct);
        }

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
