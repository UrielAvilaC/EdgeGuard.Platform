using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Hub.Application.Hl7;

public interface IHl7MonitoringService
{
    Hl7ListenerStatusDto GetListenerStatus();
    Task<IReadOnlyList<Hl7MessageSummaryDto>> GetRecentMessagesAsync(int count, CancellationToken cancellationToken = default);
    Task<Hl7MessageDetailDto?> GetMessageByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates dispatch status counts across all HL7 messages into a single summary.
    /// </summary>
    Task<QueueSummaryDto> GetQueueSummaryAsync(CancellationToken cancellationToken = default);
}
