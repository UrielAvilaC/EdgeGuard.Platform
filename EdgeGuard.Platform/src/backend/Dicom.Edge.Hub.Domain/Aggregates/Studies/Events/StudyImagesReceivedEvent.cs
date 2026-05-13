using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

public sealed record StudyImagesReceivedEvent(
    string StudyId,
    int ImageCount,
    long SizeBytes,
    string? ModalityAeTitle) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
