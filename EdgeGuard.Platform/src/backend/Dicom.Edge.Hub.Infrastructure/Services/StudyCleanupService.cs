using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Cleanup;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Evaluates cleanup policies and executes soft-delete on eligible studies in batches.
/// </summary>
public class StudyCleanupService : IStudyCleanupService
{
    private readonly IStudyCleanupPolicyRepository _policyRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StudyCleanupService> _logger;

    public StudyCleanupService(
        IStudyCleanupPolicyRepository policyRepository,
        IStudyRepository studyRepository,
        IUnitOfWork unitOfWork,
        ILogger<StudyCleanupService> logger)
    {
        _policyRepository = policyRepository;
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Study>> GetStudiesEligibleForCleanupAsync(CancellationToken ct = default)
    {
        var policies = await _policyRepository.GetEnabledAsync(ct);
        var allEligible = new List<Study>();

        foreach (var policy in policies)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-policy.RetentionDays);
            var modality = policy.Modality.ToString();

            var studies = await _studyRepository.GetStudiesForCleanupAsync(modality, cutoffDate, ct);

            _logger.LogInformation(
                "Cleanup policy for {Modality}: found {Count} studies older than {RetentionDays} days",
                modality, studies.Count, policy.RetentionDays);

            allEligible.AddRange(studies.Take(policy.MaxStudiesPerRun));
        }

        return allEligible.AsReadOnly();
    }

    public async Task<int> ExecuteCleanupAsync(int batchSize = 500, CancellationToken ct = default)
    {
        var eligible = await GetStudiesEligibleForCleanupAsync(ct);

        if (eligible.Count == 0)
            return 0;

        var totalDeleted = 0;

        foreach (var batch in eligible.Chunk(batchSize))
        {
            foreach (var study in batch)
            {
                study.SoftDelete();
                await _studyRepository.UpdateAsync(study, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            totalDeleted += batch.Length;

            _logger.LogInformation(
                "Soft-deleted batch of {BatchCount} studies (total: {TotalDeleted}/{TotalEligible})",
                batch.Length, totalDeleted, eligible.Count);
        }

        _logger.LogInformation("Study cleanup completed: {TotalDeleted} studies soft-deleted", totalDeleted);
        return totalDeleted;
    }
}
