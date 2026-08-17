namespace Dicom.Edge.Hub.Domain.Aggregates.Outbox;

/// <summary>Read access to the unified outbox activity view for monitoring.</summary>
public interface IOutboxActivityRepository
{
    /// <summary>
    /// Returns a filtered, paged slice of outbox activity (most recent first) plus the
    /// total matching count. All filters are optional.
    /// </summary>
    Task<(IReadOnlyList<OutboxActivity> Items, int Total)> QueryAsync(
        string? category,
        string? topicId,
        string? status,
        int skip,
        int take,
        CancellationToken ct = default);
}
