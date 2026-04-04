using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Infrastructure.Repositories;

public class StudyStatusAuditRepository : IStudyStatusAuditRepository
{
    private readonly HubDbContext _context;

    public StudyStatusAuditRepository(HubDbContext context) => _context = context;

    public async Task<IReadOnlyList<StudyStatusAudit>> GetByStudyAsync(string studyId, CancellationToken ct = default) =>
        await _context.StudyStatusAudits
            .Where(a => a.StudyId == studyId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyStatusAudit>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.StudyStatusAudits
            .Where(a => a.ChangedAt >= from && a.ChangedAt <= to)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(ct);

    public async Task AddAsync(StudyStatusAudit audit, CancellationToken ct = default)
    {
        await _context.StudyStatusAudits.AddAsync(audit, ct);
    }
}
