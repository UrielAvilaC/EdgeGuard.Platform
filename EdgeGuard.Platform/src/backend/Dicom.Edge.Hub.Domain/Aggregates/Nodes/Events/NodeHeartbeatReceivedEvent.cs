using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes.Events;

public sealed record NodeHeartbeatReceivedEvent(
    string NodeId,
    DateTime ReceivedAt) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
