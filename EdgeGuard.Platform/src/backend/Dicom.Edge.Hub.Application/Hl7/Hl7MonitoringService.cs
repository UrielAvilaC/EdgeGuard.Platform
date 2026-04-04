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
                m.Status,
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
                message.Status,
                message.ProcessedAt,
                message.ErrorMessage);
    }
}
