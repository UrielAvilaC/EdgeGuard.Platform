using Dicom.Edge.Hub.Domain.Entities;

namespace Dicom.Edge.Hub.Domain.Interfaces;

/// <summary>
/// Repository for HL7 messages. Supports full dispatch lifecycle queries.
/// </summary>
public interface IHl7MessageRepository
{
    Task<Hl7Message> AddAsync(Hl7Message message, CancellationToken cancellationToken = default);
    Task<Hl7Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Hl7Message>> GetByStatusAsync(Hl7MessageStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Hl7Message>> GetByDispatchStatusAsync(Hl7DispatchStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Hl7Message>> GetQueuedForDispatchAsync(int batchSize, CancellationToken cancellationToken = default);
    Task UpdateAsync(Hl7Message message, CancellationToken cancellationToken = default);
    Task<IEnumerable<Hl7Message>> GetRecentMessagesAsync(int count, CancellationToken cancellationToken = default);
    Task<int> CountByDispatchStatusAsync(Hl7DispatchStatus status, CancellationToken cancellationToken = default);
}
