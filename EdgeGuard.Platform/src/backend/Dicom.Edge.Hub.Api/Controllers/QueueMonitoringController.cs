using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.Hl7;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/queue-monitoring")]
[Authorize(Policy = Policies.ViewQueue)]
[EnableRateLimiting("api")]
public class QueueMonitoringController : ControllerBase
{
    private readonly IHl7MessageRepository _messageRepository;
    private readonly IHl7MonitoringService _monitoringService;
    private readonly ILogger<QueueMonitoringController> _logger;

    public QueueMonitoringController(
        IHl7MessageRepository messageRepository,
        IHl7MonitoringService monitoringService,
        ILogger<QueueMonitoringController> logger)
    {
        _messageRepository = messageRepository;
        _monitoringService = monitoringService;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await _monitoringService.GetQueueSummaryAsync(ct);
        return Ok(summary);
    }

    [HttpGet("by-dispatch-status/{status}")]
    public async Task<IActionResult> GetByDispatchStatus(Hl7DispatchStatus status, CancellationToken ct)
    {
        var messages = await _messageRepository.GetByDispatchStatusAsync(status, ct);
        return Ok(messages.Select(m => m.ToDto()));
    }

    [HttpGet("queued")]
    public async Task<IActionResult> GetQueued([FromQuery] int batchSize = 50, CancellationToken ct = default)
    {
        var messages = await _messageRepository.GetQueuedForDispatchAsync(batchSize, ct);
        return Ok(messages.Select(m => m.ToQueuedDto()));
    }
}
