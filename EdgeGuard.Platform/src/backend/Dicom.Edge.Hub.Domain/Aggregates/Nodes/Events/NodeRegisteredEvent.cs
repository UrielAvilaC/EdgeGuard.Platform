using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes.Events;

public sealed record NodeRegisteredEvent(
    string NodeId,
    string Name,
    string AeTitle) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
