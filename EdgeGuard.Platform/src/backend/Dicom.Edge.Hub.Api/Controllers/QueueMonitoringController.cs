using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueueMonitoringController : ControllerBase
{
    private readonly IHl7MessageRepository _messageRepository;

    public QueueMonitoringController(IHl7MessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var pending = await _messageRepository.CountByDispatchStatusAsync(Hl7DispatchStatus.PendingValidation, ct);
        var validated = await _messageRepository.CountByDispatchStatusAsync(Hl7DispatchStatus.Validated, ct);
        var routed = await _messageRepository.CountByDispatchStatusAsync(Hl7DispatchStatus.Routed, ct);
        var queued = await _messageRepository.CountByDispatchStatusAsync(Hl7DispatchStatus.Queued, ct);
        var dispatching = await _messageRepository.CountByDispatchStatusAsync(Hl7DispatchStatus.Dispatching, ct);
        var delivered = await _messageRepository.CountByDispatchStatusAsync(Hl7DispatchStatus.Delivered, ct);
        var failed = await _messageRepository.CountByDispatchStatusAsync(Hl7DispatchStatus.DeliveryFailed, ct);
        var validationFailed = await _messageRepository.CountByDispatchStatusAsync(Hl7DispatchStatus.ValidationFailed, ct);

        return Ok(new
        {
            pendingValidation = pending,
            validated,
            routed,
            queued,
            dispatching,
            delivered,
            deliveryFailed = failed,
            validationFailed,
            totalInPipeline = pending + validated + routed + queued + dispatching
        });
    }

    [HttpGet("by-dispatch-status/{status}")]
    public async Task<IActionResult> GetByDispatchStatus(Hl7DispatchStatus status, CancellationToken ct)
    {
        var messages = await _messageRepository.GetByDispatchStatusAsync(status, ct);
        return Ok(messages.Select(m => new
        {
            m.Id,
            m.MessageType,
            m.TriggerEvent,
            m.PatientId,
            m.PatientName,
            m.SendingFacility,
            Status = m.Status.ToString(),
            DispatchStatus = m.DispatchStatus.ToString(),
            m.TargetNodeId,
            m.TargetNodeName,
            m.Priority,
            m.DispatchAttempts,
            m.DispatchError,
            m.ReceivedAt,
            m.ValidatedAt,
            m.RoutedAt,
            m.QueuedAt,
            m.DispatchedAt,
            m.DeliveredAt
        }));
    }

    [HttpGet("queued")]
    public async Task<IActionResult> GetQueued([FromQuery] int batchSize = 50, CancellationToken ct = default)
    {
        var messages = await _messageRepository.GetQueuedForDispatchAsync(batchSize, ct);
        return Ok(messages.Select(m => new
        {
            m.Id,
            m.MessageType,
            m.TriggerEvent,
            m.PatientId,
            m.TargetNodeId,
            m.TargetNodeName,
            m.Priority,
            m.ReceivedAt,
            m.QueuedAt
        }));
    }
}
