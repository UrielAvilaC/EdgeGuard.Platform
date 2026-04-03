using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

public sealed record StudyWorklistReadEvent(
    string StudyId,
    string StudyInstanceUid,
    string ModalityAeTitle) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
