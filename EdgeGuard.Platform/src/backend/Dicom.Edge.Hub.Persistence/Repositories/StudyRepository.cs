using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class StudyRepository : IStudyRepository
{
    private readonly HubDbContext _context;

    public StudyRepository(HubDbContext context) => _context = context;

    public async Task<Study?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Studies
            .Include(s => s.Series)
            .Include(s => s.StatusAudits)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Study?> GetByStudyInstanceUidAsync(string studyInstanceUid, CancellationToken ct = default) =>
        await _context.Studies
            .Include(s => s.Series)
            .Include(s => s.StatusAudits)
            .FirstOrDefaultAsync(s => s.StudyInstanceUid.Value == studyInstanceUid, ct);

    public async Task<IReadOnlyList<Study>> GetByPatientIdAsync(string patientId, CancellationToken ct = default) =>
        await _context.Studies
            .Where(s => s.PatientId == patientId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Study>> GetByNodeAsync(string nodeId, CancellationToken ct = default) =>
        await _context.Studies
            .Where(s => s.SourceNodeId == nodeId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Study>> GetByStatusAsync(StudyStatus status, CancellationToken ct = default) =>
        await _context.Studies
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Study>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.Studies
            .Where(s => s.StudyDate >= from && s.StudyDate <= to)
            .OrderByDescending(s => s.StudyDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Study>> GetPendingForPacsAsync(CancellationToken ct = default) =>
        await _context.Studies
            .Where(s => s.Status == StudyStatus.QueuedForSend || s.Status == StudyStatus.Completed)
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Study>> GetStudiesForCleanupAsync(string modality, DateTime olderThan, CancellationToken ct = default) =>
        await _context.Studies
            .Include(s => s.Series)
            .Where(s => s.Series.Any(ss => ss.Modality == modality) && s.CreatedAt < olderThan)
            .ToListAsync(ct);

    public async Task<Study> AddAsync(Study study, CancellationToken ct = default)
    {
        await _context.Studies.AddAsync(study, ct);
        return study;
    }

    public Task UpdateAsync(Study study, CancellationToken ct = default)
    {
        _context.Studies.Update(study);
        return Task.CompletedTask;
    }

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _context.Studies.CountAsync(ct);
}
