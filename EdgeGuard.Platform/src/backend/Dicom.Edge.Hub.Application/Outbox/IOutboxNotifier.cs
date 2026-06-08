namespace Dicom.Edge.Hub.Application.Outbox;

/// <summary>One real-time outbox state change, broadcast to the monitoring UI.</summary>
public sealed record OutboxEntryChange(
    string Category,
    string Id,
    string TopicId,
    string Status,
    int Attempts,
    string? Error);

/// <summary>
/// Publishes outbox state changes to subscribers (SignalR). Defined in Application so the
/// dispatchers depend only on the abstraction; the transport impl lives in the API layer.
/// </summary>
public interface IOutboxNotifier
{
    Task EntryChangedAsync(OutboxEntryChange change, CancellationToken ct = default);
}
