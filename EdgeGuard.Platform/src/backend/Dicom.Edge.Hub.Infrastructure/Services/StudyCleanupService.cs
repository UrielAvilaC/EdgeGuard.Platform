using Dicom.Edge.Hub.Domain.Aggregates.Cleanup;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Evaluates cleanup policies and returns studies eligible for deletion.
/// </summary>
public class StudyCleanupService : IStudyCleanupService
{
    private readonly IStudyCleanupPolicyRepository _policyRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly ILogger<StudyCleanupService> _logger;

    public StudyCleanupService(
        IStudyCleanupPolicyRepository policyRepository,
        IStudyRepository studyRepository,
        ILogger<StudyCleanupService> logger)
    {
        _policyRepository = policyRepository;
        _studyRepository = studyRepository;
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
}
