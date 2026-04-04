using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.Hl7;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Hl7StatusController : ControllerBase
{
    private readonly IHl7MonitoringService _monitoringService;

    public Hl7StatusController(IHl7MonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    /// <summary>
    /// Obtiene el estado del listener HL7.
    /// </summary>
    [HttpGet("status")]
    public ActionResult<Hl7ListenerStatusDto> GetStatus()
    {
        var status = _monitoringService.GetListenerStatus();
        return Ok(status);
    }

    /// <summary>
    /// Obtiene los mensajes recientes recibidos.
    /// </summary>
    [HttpGet("recent-messages")]
    public async Task<ActionResult<IReadOnlyList<Hl7MessageSummaryDto>>> GetRecentMessages([FromQuery] int count = 10)
    {
        var messages = await _monitoringService.GetRecentMessagesAsync(count, HttpContext.RequestAborted);
        return Ok(messages);
    }

    /// <summary>
    /// Obtiene un mensaje por ID.
    /// </summary>
    [HttpGet("messages/{id:guid}")]
    public async Task<ActionResult<Hl7MessageDetailDto>> GetMessage(Guid id)
    {
        var message = await _monitoringService.GetMessageByIdAsync(id, HttpContext.RequestAborted);
        return message is null ? NotFound() : Ok(message);
    }
}
