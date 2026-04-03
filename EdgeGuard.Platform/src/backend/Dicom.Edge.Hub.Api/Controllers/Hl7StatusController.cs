using Dicom.Edge.Hub.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Hl7StatusController : ControllerBase
{
    private readonly IHl7Listener _listener;
    private readonly IHl7MessageRepository _repository;
    private readonly ILogger<Hl7StatusController> _logger;

    public Hl7StatusController(
        IHl7Listener listener,
        IHl7MessageRepository repository,
        ILogger<Hl7StatusController> logger)
    {
        _listener = listener;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado del listener HL7.
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            isRunning = _listener.IsRunning,
            port = _listener.Port,
            activeConnections = _listener.ActiveConnections
        });
    }

    /// <summary>
    /// Obtiene los mensajes recientes recibidos.
    /// </summary>
    [HttpGet("recent-messages")]
    public async Task<IActionResult> GetRecentMessages([FromQuery] int count = 10)
    {
        var messages = await _repository.GetRecentMessagesAsync(count);
        
        return Ok(messages.Select(m => new
        {
            m.Id,
            m.MessageType,
            m.SendingApplication,
            m.SendingFacility,
            m.ReceivedAt,
            m.ClientEndpoint,
            m.Status,
            m.ProcessedAt,
            m.ErrorMessage
        }));
    }

    /// <summary>
    /// Obtiene un mensaje por ID.
    /// </summary>
    [HttpGet("messages/{id}")]
    public async Task<IActionResult> GetMessage(Guid id)
    {
        var message = await _repository.GetByIdAsync(id);
        
        if (message == null)
            return NotFound();

        return Ok(new
        {
            message.Id,
            message.Content,
            message.MessageType,
            message.SendingApplication,
            message.SendingFacility,
            message.ReceivedAt,
            message.ClientEndpoint,
            message.Status,
            message.ProcessedAt,
            message.ErrorMessage
        });
    }
}
