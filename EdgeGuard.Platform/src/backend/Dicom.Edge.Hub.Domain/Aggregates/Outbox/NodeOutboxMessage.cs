using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Outbox;

/// <summary>
/// Durable outbox row for a control-plane push to one Edge Node (config, rules, PACS,
/// equipment). Written by the producing application service and drained by the node outbox
/// dispatcher with at-least-once delivery, exponential backoff and dead-lettering — the
/// durable replacement for the former in-memory <c>NodePushQueue</c>.
/// </summary>
public sealed class NodeOutboxMessage : Entity<string>
{
    /// <summary>FK to <c>outbox_topics</c> (one of the <c>node.*</c> keys).</summary>
    public string TopicId { get; private set; } = default!;
    public string NodeId { get; private set; } = default!;
    public NodeOutboxStatus Status { get; private set; }
    public int Attempts { get; private set; }
    /// <summary>Earliest time this row may be (re)dispatched — drives the retry backoff.</summary>
    public DateTime? NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? SentAt { get; private set; }

    private NodeOutboxMessage() { }

    public static NodeOutboxMessage Create(string nodeId, string topicId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node ID cannot be empty.", nameof(nodeId));
        if (string.IsNullOrWhiteSpace(topicId))
            throw new ArgumentException("Topic ID cannot be empty.", nameof(topicId));

        return new NodeOutboxMessage
        {
            Id = IdGenerator.NewId(),
            NodeId = nodeId.Trim(),
            TopicId = topicId.Trim(),
            Status = NodeOutboxStatus.Pending,
            Attempts = 0
        };
    }

    public void MarkSent()
    {
        Status = NodeOutboxStatus.Sent;
        SentAt = DateTime.UtcNow;
        Attempts++;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = NodeOutboxStatus.Failed;
        Attempts++;
        LastError = error?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Manually returns the row to Pending for immediate re-dispatch.</summary>
    public void Requeue()
    {
        Status = NodeOutboxStatus.Pending;
        NextAttemptAt = null;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Returns the row to Pending with a future <see cref="NextAttemptAt"/> (backoff).</summary>
    public void ScheduleRetry(TimeSpan delay, string error)
    {
        Status = NodeOutboxStatus.Pending;
        Attempts++;
        LastError = error?.Trim();
        NextAttemptAt = DateTime.UtcNow.Add(delay);
        UpdatedAt = DateTime.UtcNow;
    }
}
