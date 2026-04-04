namespace Dicom.Edge.Hub.Domain.Aggregates.Cleanup;

/// <summary>
/// Repository interface for StudyCleanupPolicy aggregate.
/// </summary>
public interface IStudyCleanupPolicyRepository
{
    Task<StudyCleanupPolicy?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<StudyCleanupPolicy?> GetByModalityAsync(string modality, CancellationToken ct = default);
    Task<IReadOnlyList<StudyCleanupPolicy>> GetEnabledAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StudyCleanupPolicy>> GetAllAsync(CancellationToken ct = default);
    Task<StudyCleanupPolicy> AddAsync(StudyCleanupPolicy policy, CancellationToken ct = default);
    Task UpdateAsync(StudyCleanupPolicy policy, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
