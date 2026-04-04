using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

public sealed record StudySentToPacsEvent(
    string StudyId,
    string StudyInstanceUid,
    string? TargetPacsId) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
