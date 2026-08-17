using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

/// <summary>
/// Raised when a study moves along the PACS-send axis. Kept separate from
/// <see cref="StudyStatusChangedEvent"/>, which carries the clinical lifecycle: subscribers that
/// care about results (notification rules) must not be woken by transport progress, and vice versa.
/// </summary>
public sealed record StudyPacsStatusChangedEvent(
    string StudyId,
    StudyPacsStatus OldStatus,
    StudyPacsStatus NewStatus,
    string? Reason) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
