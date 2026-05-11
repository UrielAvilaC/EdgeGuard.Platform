using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;

namespace Dicom.Edge.Hub.Application.Hl7;

public sealed class Hl7MonitoringService : IHl7MonitoringService
{
    private readonly IHl7Listener _listener;
    private readonly IHl7MessageRepository _repository;

    public Hl7MonitoringService(IHl7Listener listener, IHl7MessageRepository repository)
    {
        _listener = listener;
        _repository = repository;
    }

    public Hl7ListenerStatusDto GetListenerStatus() =>
        new(_listener.IsRunning, _listener.Port, _listener.ActiveConnections);

    public async Task<IReadOnlyList<Hl7MessageSummaryDto>> GetRecentMessagesAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        var normalizedCount = Math.Clamp(count, 1, 100);
        var messages = await _repository.GetRecentMessagesAsync(normalizedCount, cancellationToken);

        return messages
            .Select(m => new Hl7MessageSummaryDto(
                m.Id,
                m.MessageType,
                m.SendingApplication,
                m.SendingFacility,
                m.ReceivedAt,
                m.ClientEndpoint,
                m.Status.ToString(),
                m.ProcessedAt,
                m.ErrorMessage))
            .ToList();
    }

    public async Task<Hl7MessageDetailDto?> GetMessageByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _repository.GetByIdAsync(id, cancellationToken);

        return message is null
            ? null
            : new Hl7MessageDetailDto(
                message.Id,
                message.Content,
                message.MessageType,
                message.SendingApplication,
                message.SendingFacility,
                message.ReceivedAt,
                message.ClientEndpoint,
                message.Status.ToString(),
                message.ProcessedAt,
                message.ErrorMessage);
    }

    public async Task<QueueSummaryDto> GetQueueSummaryAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _repository.CountByDispatchStatusAsync(Hl7DispatchStatus.PendingValidation, cancellationToken);
        var validated = await _repository.CountByDispatchStatusAsync(Hl7DispatchStatus.Validated, cancellationToken);
        var routed = await _repository.CountByDispatchStatusAsync(Hl7DispatchStatus.Routed, cancellationToken);
        var queued = await _repository.CountByDispatchStatusAsync(Hl7DispatchStatus.Queued, cancellationToken);
        var dispatching = await _repository.CountByDispatchStatusAsync(Hl7DispatchStatus.Dispatching, cancellationToken);
        var delivered = await _repository.CountByDispatchStatusAsync(Hl7DispatchStatus.Delivered, cancellationToken);
        var failed = await _repository.CountByDispatchStatusAsync(Hl7DispatchStatus.DeliveryFailed, cancellationToken);
        var validationFailed = await _repository.CountByDispatchStatusAsync(Hl7DispatchStatus.ValidationFailed, cancellationToken);

        return new QueueSummaryDto
        {
            PendingValidation = pending,
            Validated = validated,
            Routed = routed,
            Queued = queued,
            Dispatching = dispatching,
            Delivered = delivered,
            DeliveryFailed = failed,
            ValidationFailed = validationFailed,
            TotalInPipeline = pending + validated + routed + queued + dispatching
        };
    }
}
