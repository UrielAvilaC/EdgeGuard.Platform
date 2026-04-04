using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

public sealed record StudyReleasedEvent(
    string StudyId,
    string StudyInstanceUid,
    string? ReleasedByNodeId) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
