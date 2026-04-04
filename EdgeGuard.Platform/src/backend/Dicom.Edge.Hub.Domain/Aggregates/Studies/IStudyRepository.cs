using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies;

/// <summary>
/// Repository interface for Study aggregate.
/// </summary>
public interface IStudyRepository
{
    Task<Study?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Study?> GetByStudyInstanceUidAsync(string studyInstanceUid, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetByPatientIdAsync(string patientId, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetByNodeAsync(string nodeId, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetByStatusAsync(StudyStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetPendingForPacsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetStudiesForCleanupAsync(string modality, DateTime olderThan, CancellationToken ct = default);
    Task<Study> AddAsync(Study study, CancellationToken ct = default);
    Task UpdateAsync(Study study, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
