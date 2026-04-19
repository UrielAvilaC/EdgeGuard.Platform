using System.Linq.Expressions;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Common.Sorting;
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
            .AsNoTracking()
            .Where(s => s.PatientId == patientId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Study>> GetByNodeAsync(string nodeId, CancellationToken ct = default) =>
        await _context.Studies
            .AsNoTracking()
            .Where(s => s.SourceNodeId == nodeId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Study>> GetByStatusAsync(StudyStatus status, CancellationToken ct = default) =>
        await _context.Studies
            .AsNoTracking()
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Study>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.Studies
            .AsNoTracking()
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

    public async Task<PagedResult<Study>> GetPagedAsync(PaginationRequest pagination, CancellationToken ct = default)
    {
        var query = _context.Studies.AsNoTracking().OrderByDescending(s => s.CreatedAt);
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct);

        return new PagedResult<Study>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

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

    public async Task<PagedResult<Study>> GetFilteredPagedAsync(
        PaginationRequest pagination,
        StudyFilterCriteria filter,
        CancellationToken ct = default)
    {
        StudyStatus? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<StudyStatus>(filter.Status, true, out var s))
            status = s;

        var query = BuildFilteredQuery(filter.Search, status, filter.SourceNodeId, filter.PatientId, filter.DateFrom, filter.DateTo, filter.IsUrgent);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .ApplySort(filter.SortBy, filter.SortDir, StudySortFields, q => q.OrderByDescending(x => x.CreatedAt))
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Study>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<Study>> GetFilteredAllAsync(
        StudyFilterCriteria filter,
        CancellationToken ct = default)
    {
        StudyStatus? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<StudyStatus>(filter.Status, true, out var s))
            status = s;

        return await BuildFilteredQuery(filter.Search, status, filter.SourceNodeId, filter.PatientId, filter.DateFrom, filter.DateTo, filter.IsUrgent)
            .ApplySort(filter.SortBy, filter.SortDir, StudySortFields, q => q.OrderByDescending(x => x.CreatedAt))
            .ToListAsync(ct);
    }

    private static readonly Dictionary<string, Expression<Func<Study, object?>>> StudySortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["createdAt"] = s => s.CreatedAt,
        ["updatedAt"] = s => s.UpdatedAt,
        ["studyDate"] = s => s.StudyDate,
        ["patientName"] = s => s.PatientName,
        ["accessionNumber"] = s => s.AccessionNumber,
        ["status"] = s => s.Status,
        ["instanceCount"] = s => s.InstanceCount,
        ["totalSizeBytes"] = s => s.TotalSizeBytes,
        ["priority"] = s => s.Priority,
        ["sourceNodeId"] = s => s.SourceNodeId,
        ["studyDescription"] = s => s.StudyDescription,
    };

    private IQueryable<Study> BuildFilteredQuery(
        string? search, StudyStatus? status, string? sourceNodeId,
        string? patientId, DateTime? dateFrom, DateTime? dateTo, bool? isUrgent)
    {
        var query = _context.Studies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                (s.AccessionNumber != null && s.AccessionNumber.Contains(search)) ||
                (s.PatientName != null && s.PatientName.Contains(search)) ||
                (s.StudyDescription != null && s.StudyDescription.Contains(search)));

        if (status.HasValue) query = query.Where(s => s.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(sourceNodeId)) query = query.Where(s => s.SourceNodeId == sourceNodeId);
        if (!string.IsNullOrWhiteSpace(patientId)) query = query.Where(s => s.PatientId == patientId);
        if (dateFrom.HasValue) query = query.Where(s => s.StudyDate >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(s => s.StudyDate <= dateTo.Value);
        if (isUrgent.HasValue) query = query.Where(s => s.IsUrgent == isUrgent.Value);

        return query;
    }
}
