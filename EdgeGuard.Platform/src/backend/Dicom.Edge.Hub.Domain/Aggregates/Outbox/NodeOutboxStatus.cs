namespace Dicom.Edge.Hub.Domain.Aggregates.Outbox;

/// <summary>Lifecycle of a node-sync outbox message.</summary>
public enum NodeOutboxStatus
{
    /// <summary>Awaiting dispatch (or scheduled for retry via <c>NextAttemptAt</c>).</summary>
    Pending,

    /// <summary>Successfully pushed to the node.</summary>
    Sent,

    /// <summary>Permanently failed after exhausting retries (dead-lettered).</summary>
    Failed
}
