using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Services;

/// <summary>
/// Domain service for recording study status change audits.
/// </summary>
public interface IStudyAuditService
{
    Task RecordStatusChangeAsync(
        string studyId,
        StudyStatus? previousStatus,
        StudyStatus newStatus,
        string? changedByNodeId = null,
        string? reason = null,
        CancellationToken ct = default);
}
