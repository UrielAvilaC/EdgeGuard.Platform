namespace Dicom.Edge.Hub.Application.Hl7;

public interface IHl7MonitoringService
{
    Hl7ListenerStatusDto GetListenerStatus();
    Task<IReadOnlyList<Hl7MessageSummaryDto>> GetRecentMessagesAsync(int count, CancellationToken cancellationToken = default);
    Task<Hl7MessageDetailDto?> GetMessageByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
