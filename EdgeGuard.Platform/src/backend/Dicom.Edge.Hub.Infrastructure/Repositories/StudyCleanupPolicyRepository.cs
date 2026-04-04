using Dicom.Edge.Hub.Domain.Aggregates.Cleanup;
using Dicom.Edge.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Infrastructure.Repositories;

public class StudyCleanupPolicyRepository : IStudyCleanupPolicyRepository
{
    private readonly HubDbContext _context;

    public StudyCleanupPolicyRepository(HubDbContext context) => _context = context;

    public async Task<StudyCleanupPolicy?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.StudyCleanupPolicies.FindAsync([id], ct);

    public async Task<StudyCleanupPolicy?> GetByModalityAsync(string modality, CancellationToken ct = default) =>
        await _context.StudyCleanupPolicies
            .FirstOrDefaultAsync(p => p.Modality.ToString() == modality, ct);

    public async Task<IReadOnlyList<StudyCleanupPolicy>> GetEnabledAsync(CancellationToken ct = default) =>
        await _context.StudyCleanupPolicies
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyCleanupPolicy>> GetAllAsync(CancellationToken ct = default) =>
        await _context.StudyCleanupPolicies
            .OrderBy(p => p.Priority)
            .ToListAsync(ct);

    public async Task<StudyCleanupPolicy> AddAsync(StudyCleanupPolicy policy, CancellationToken ct = default)
    {
        await _context.StudyCleanupPolicies.AddAsync(policy, ct);
        return policy;
    }

    public Task UpdateAsync(StudyCleanupPolicy policy, CancellationToken ct = default)
    {
        _context.StudyCleanupPolicies.Update(policy);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var policy = await _context.StudyCleanupPolicies.FindAsync([id], ct);
        if (policy is not null)
            _context.StudyCleanupPolicies.Remove(policy);
    }
}
