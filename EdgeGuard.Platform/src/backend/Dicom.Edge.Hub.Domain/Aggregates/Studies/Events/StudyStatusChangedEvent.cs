using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

public sealed record StudyStatusChangedEvent(
    string StudyId,
    StudyStatus OldStatus,
    StudyStatus NewStatus,
    string? Reason) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
