using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

/// <summary>
/// Raised when a study enters the <see cref="Dicom.Edge.Models.Enums.StudyStatus.Scheduled"/>
/// state — either created from an HL7 worklist (ORM/SIU) or re-scheduled. Drives auto-send
/// rules bound to the "Agenda" clinical status.
/// </summary>
public sealed record StudyScheduledEvent(
    string StudyId,
    string? AccessionNumber) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
