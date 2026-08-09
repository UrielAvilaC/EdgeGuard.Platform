namespace Dicom.Edge.Hub.Domain.Aggregates.Outbox;

/// <summary>Read/write access to the <c>outbox_topics</c> catalog.</summary>
public interface IOutboxTopicRepository
{
    Task<OutboxTopic?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxTopic>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(OutboxTopic topic, CancellationToken ct = default);
}
