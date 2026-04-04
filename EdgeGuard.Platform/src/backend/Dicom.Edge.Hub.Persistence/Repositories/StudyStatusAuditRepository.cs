using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class StudyStatusAuditRepository : IStudyStatusAuditRepository
{
    private readonly HubDbContext _context;

    public StudyStatusAuditRepository(HubDbContext context) => _context = context;

    public async Task<IReadOnlyList<StudyStatusAudit>> GetByStudyAsync(string studyId, CancellationToken ct = default) =>
        await _context.StudyStatusAudits
            .AsNoTracking()
            .Where(a => a.StudyId == studyId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyStatusAudit>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.StudyStatusAudits
            .AsNoTracking()
            .Where(a => a.ChangedAt >= from && a.ChangedAt <= to)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(ct);

    public async Task AddAsync(StudyStatusAudit audit, CancellationToken ct = default)
    {
        await _context.StudyStatusAudits.AddAsync(audit, ct);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default)
    {
        var total = 0;
        int deleted;
        do
        {
            deleted = await _context.StudyStatusAudits
                .Where(a => a.ChangedAt < cutoff)
                .OrderBy(a => a.ChangedAt)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);
            total += deleted;
        } while (deleted == batchSize && !ct.IsCancellationRequested);
        return total;
    }
}
