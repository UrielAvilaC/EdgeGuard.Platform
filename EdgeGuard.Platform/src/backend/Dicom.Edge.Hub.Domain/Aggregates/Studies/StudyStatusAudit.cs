using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies;

/// <summary>
/// Immutable audit record for each study status change.
/// </summary>
public sealed class StudyStatusAudit : Entity<string>
{
    public string StudyId { get; private set; } = default!;
    public StudyStatus? PreviousStatus { get; private set; }
    public StudyStatus NewStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? ChangedByNodeId { get; private set; }
    public string? Reason { get; private set; }

    private StudyStatusAudit() { }

    public static StudyStatusAudit Create(
        string studyId,
        StudyStatus? previousStatus,
        StudyStatus newStatus,
        string? changedByNodeId = null,
        string? reason = null)
    {
        return new StudyStatusAudit
        {
            Id = IdGenerator.NewId(),
            StudyId = studyId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedByNodeId = changedByNodeId,
            Reason = reason
        };
    }
}
