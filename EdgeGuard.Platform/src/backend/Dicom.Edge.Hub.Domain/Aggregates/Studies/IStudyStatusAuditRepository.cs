namespace Dicom.Edge.Hub.Domain.Aggregates.Studies;

/// <summary>
/// Repository interface for study status audit records.
/// </summary>
public interface IStudyStatusAuditRepository
{
    Task<IReadOnlyList<StudyStatusAudit>> GetByStudyAsync(string studyId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyStatusAudit>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(StudyStatusAudit audit, CancellationToken ct = default);
}
