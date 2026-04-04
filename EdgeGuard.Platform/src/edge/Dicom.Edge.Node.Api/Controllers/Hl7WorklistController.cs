using Dicom.Edge.Contracts.Hl7;
using Dicom.Edge.Node.Worklist;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Receives HL7 worklist pushes from the Hub.
/// </summary>
[ApiController]
[Route("api/hl7")]
public sealed class Hl7WorklistController(
    IWorklistManager worklistManager,
    ILogger<Hl7WorklistController> logger) : ControllerBase
{
    /// <summary>
    /// POST /api/hl7/worklist — Hub pushes an HL7 worklist item to this node.
    /// </summary>
    [HttpPost("worklist")]
    public async Task<IActionResult> AcceptWorklist(
        [FromBody] Hl7WorklistPushRequest request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Worklist push received from Hub: MessageId={HubMessageId}, Type={MessageType}",
            request.HubMessageId, request.MessageType);

        var result = await worklistManager.AcceptWorklistItemAsync(request, ct);

        if (!result.Accepted)
        {
            return UnprocessableEntity(Hl7WorklistPushResponse.Reject(result.Error ?? "Unknown error"));
        }

        return Ok(Hl7WorklistPushResponse.Accept(result.AckId!));
    }
}
