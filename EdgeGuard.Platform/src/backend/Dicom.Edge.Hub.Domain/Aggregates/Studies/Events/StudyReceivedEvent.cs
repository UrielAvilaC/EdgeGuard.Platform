using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

public sealed record StudyReceivedEvent(
    string StudyId,
    string StudyInstanceUid,
    string? PatientId,
    string? SourceNodeId) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
