using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes.Events;

public sealed record NodeStatusChangedEvent(
    string NodeId,
    NodeStatus OldStatus,
    NodeStatus NewStatus) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
