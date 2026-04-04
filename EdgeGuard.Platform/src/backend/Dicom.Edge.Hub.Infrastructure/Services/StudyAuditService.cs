using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Services;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Records study status audit entries.
/// </summary>
public class StudyAuditService : IStudyAuditService
{
    private readonly IStudyStatusAuditRepository _auditRepository;

    public StudyAuditService(IStudyStatusAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task RecordStatusChangeAsync(
        string studyId,
        StudyStatus? previousStatus,
        StudyStatus newStatus,
        string? changedByNodeId = null,
        string? reason = null,
        CancellationToken ct = default)
    {
        var audit = StudyStatusAudit.Create(studyId, previousStatus, newStatus, changedByNodeId, reason);
        await _auditRepository.AddAsync(audit, ct);
    }
}
