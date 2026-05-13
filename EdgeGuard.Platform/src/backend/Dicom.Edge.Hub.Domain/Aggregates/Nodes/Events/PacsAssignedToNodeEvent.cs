using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes.Events;

public sealed record PacsAssignedToNodeEvent(
    string NodeId,
    string PacsId,
    bool InheritedFromHub) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
