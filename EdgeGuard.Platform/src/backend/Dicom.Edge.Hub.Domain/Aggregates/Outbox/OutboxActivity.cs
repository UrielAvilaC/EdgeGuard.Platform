namespace Dicom.Edge.Hub.Domain.Aggregates.Outbox;

/// <summary>
/// Read-only, keyless projection over the unified <c>vw_outbox_activity</c> view
/// (UNION of <c>notifications</c> and <c>node_outbox_messages</c> joined to
/// <c>outbox_topics</c>). Powers the outbox monitoring UI; not an aggregate.
/// </summary>
public sealed class OutboxActivity
{
    public string Id { get; init; } = default!;
    /// <summary><c>NodeSync</c> or <c>Notification</c>.</summary>
    public string Category { get; init; } = default!;
    public string TopicId { get; init; } = default!;
    public string TopicName { get; init; } = default!;
    /// <summary>Destination: node id (node-sync) or recipient email/phone (notification).</summary>
    public string? Target { get; init; }
    public string Status { get; init; } = default!;
    public int Attempts { get; init; }
    public DateTime? NextAttemptAt { get; init; }
    public string? LastError { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}
