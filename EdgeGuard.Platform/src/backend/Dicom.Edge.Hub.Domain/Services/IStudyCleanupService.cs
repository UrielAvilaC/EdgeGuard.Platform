using Dicom.Edge.Hub.Domain.Aggregates.Studies;

namespace Dicom.Edge.Hub.Domain.Services;

/// <summary>
/// Domain service for evaluating and executing study cleanup policies by modality and age.
/// </summary>
public interface IStudyCleanupService
{
    /// <summary>
    /// Returns studies eligible for cleanup based on active policies.
    /// </summary>
    Task<IReadOnlyList<Study>> GetStudiesEligibleForCleanupAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes cleanup: soft-deletes eligible studies in batches and returns the total count deleted.
    /// </summary>
    Task<int> ExecuteCleanupAsync(int batchSize = 500, CancellationToken ct = default);
}
