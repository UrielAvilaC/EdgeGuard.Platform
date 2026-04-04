using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class PacsSendAuditRepository : IPacsSendAuditRepository
{
    private readonly HubDbContext _context;

    public PacsSendAuditRepository(HubDbContext context) => _context = context;

    public async Task<PacsSendAudit> AddAsync(PacsSendAudit audit, CancellationToken ct = default)
    {
        await _context.PacsSendAudits.AddAsync(audit, ct);
        return audit;
    }

    public async Task<IReadOnlyList<PacsSendAudit>> GetByStudyAsync(string studyId, CancellationToken ct = default) =>
        await _context.PacsSendAudits
            .AsNoTracking()
            .Where(a => a.StudyId == studyId)
            .OrderByDescending(a => a.SentAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PacsSendAudit>> GetByPacsAsync(string pacsId, int limit = 100, CancellationToken ct = default) =>
        await _context.PacsSendAudits
            .AsNoTracking()
            .Where(a => a.PacsId == pacsId)
            .OrderByDescending(a => a.SentAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<PacsSendAudit?> GetLatestByStudyAsync(string studyId, CancellationToken ct = default) =>
        await _context.PacsSendAudits
            .AsNoTracking()
            .Where(a => a.StudyId == studyId)
            .OrderByDescending(a => a.SentAt)
            .FirstOrDefaultAsync(ct);

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default)
    {
        var total = 0;
        int deleted;
        do
        {
            deleted = await _context.PacsSendAudits
                .Where(a => a.SentAt < cutoff)
                .OrderBy(a => a.SentAt)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);
            total += deleted;
        } while (deleted == batchSize && !ct.IsCancellationRequested);
        return total;
    }
}
