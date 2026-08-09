using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Node.Queue;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Receives manual study operations pushed from the Hub.
/// </summary>
[ApiController]
[Route("api/studies")]
public sealed class StudiesController(
    INodeWorkQueue workQueue,
    ILogger<StudiesController> logger) : ControllerBase
{
    /// <summary>POST /api/studies/requeue — manual resend to explicit PACS destinations.</summary>
    [HttpPost("requeue")]
    public async Task<IActionResult> Requeue([FromBody] StudyRequeueRequest payload, CancellationToken ct)
    {
        logger.LogInformation(
            "Manual requeue received: StudyUid={StudyUid}, PacsIds=[{PacsIds}]",
            payload.StudyInstanceUid, string.Join(", ", payload.PacsIds));

        var result = await workQueue.RequeueStudyAsync(payload.StudyInstanceUid, payload.PacsIds, ct);

        if (result.IsFailure)
        {
            return NotFound(new StudyRequeueResponse
            {
                Accepted = false,
                AppliedAtUtc = DateTime.UtcNow,
                Error = result.Error?.Message,
            });
        }

        return Ok(new StudyRequeueResponse
        {
            Accepted = true,
            TargetCount = payload.PacsIds.Count,
            AppliedAtUtc = DateTime.UtcNow,
        });
    }
}
