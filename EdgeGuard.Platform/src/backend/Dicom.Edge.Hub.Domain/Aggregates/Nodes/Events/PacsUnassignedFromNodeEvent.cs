using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes.Events;

public sealed record PacsUnassignedFromNodeEvent(
    string NodeId,
    string PacsId) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
