using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

public sealed record StudyCompletedEvent(
    string StudyId,
    string StudyInstanceUid,
    int TotalInstances,
    long TotalSizeBytes) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
