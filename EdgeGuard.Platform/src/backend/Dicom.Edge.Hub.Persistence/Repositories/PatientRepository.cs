using System.Linq.Expressions;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Common.Sorting;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly HubDbContext _context;

    public PatientRepository(HubDbContext context) => _context = context;

    public async Task<Patient?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Patients.FindAsync([id], ct);

    public async Task<Patient?> GetByPatientDicomIdAsync(string patientDicomId, CancellationToken ct = default) =>
        await _context.Patients
            .FirstOrDefaultAsync(p => p.PatientDicomId.Value == patientDicomId, ct);

    public async Task<IReadOnlyList<Patient>> FindByNameAsync(string name, CancellationToken ct = default) =>
        await _context.Patients
            .AsNoTracking()
            .Where(p => p.PatientName.Contains(name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Patient>> GetByNodeAsync(string nodeId, CancellationToken ct = default) =>
        await _context.Patients
            .AsNoTracking()
            .Where(p => p.CreatedByNodeId == nodeId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Patient>> GetActiveAsync(CancellationToken ct = default) =>
        await _context.Patients
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(ct);

    public async Task<PagedResult<Patient>> GetPagedAsync(PaginationRequest pagination, CancellationToken ct = default)
    {
        var query = _context.Patients.AsNoTracking().OrderBy(p => p.PatientName);
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct);

        return new PagedResult<Patient>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Patient> AddAsync(Patient patient, CancellationToken ct = default)
    {
        await _context.Patients.AddAsync(patient, ct);
        await _context.SaveChangesAsync();
        return patient;
    }

    public Task UpdateAsync(Patient patient, CancellationToken ct = default)
    {
        _context.Patients.Update(patient);
        return Task.CompletedTask;
    }

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _context.Patients.CountAsync(ct);

    /// <summary>
    /// P0-7: Returns patients whose <c>MergedIntoPatientId</c> equals the given prior ID.
    /// Bypasses the soft-delete query filter — chain-collapsing must update deactivated
    /// records too.
    /// </summary>
    public async Task<IReadOnlyList<Patient>> GetByMergedIntoPatientIdAsync(
        string priorPatientDicomId, CancellationToken ct = default) =>
        await _context.Patients
            .IgnoreQueryFilters()
            .Where(p => p.MergedIntoPatientId == priorPatientDicomId)
            .ToListAsync(ct);

    public async Task<PagedResult<Patient>> GetFilteredPagedAsync(
        PaginationRequest pagination,
        PatientFilterCriteria filter,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(filter.Search, filter.CreatedByNodeId, filter.IsActive, filter.HasPhone, filter.HasEmail);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .ApplySort(filter.SortBy, filter.SortDir, PatientSortFields, q => q.OrderBy(p => p.PatientName))
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Patient>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<Patient>> GetFilteredAllAsync(
        PatientFilterCriteria filter,
        CancellationToken ct = default)
    {
        return await BuildFilteredQuery(filter.Search, filter.CreatedByNodeId, filter.IsActive, filter.HasPhone, filter.HasEmail)
            .ApplySort(filter.SortBy, filter.SortDir, PatientSortFields, q => q.OrderBy(p => p.PatientName))
            .ToListAsync(ct);
    }

    private static readonly Dictionary<string, Expression<Func<Patient, object?>>> PatientSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["patientName"] = p => p.PatientName,
        ["createdAt"] = p => p.CreatedAt,
        ["updatedAt"] = p => p.UpdatedAt,
        ["birthDate"] = p => p.BirthDate,
        ["sex"] = p => p.Sex,
        ["patientDicomId"] = p => p.PatientDicomId.Value,
        ["isActive"] = p => p.IsActive,
    };

    private IQueryable<Patient> BuildFilteredQuery(
        string? search, string? createdByNodeId, bool? isActive, bool? hasPhone, bool? hasEmail)
    {
        var query = _context.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.PatientName.Contains(search) ||
                p.PatientDicomId.Value.Contains(search));

        if (!string.IsNullOrWhiteSpace(createdByNodeId)) query = query.Where(p => p.CreatedByNodeId == createdByNodeId);
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);
        if (hasPhone == true) query = query.Where(p => p.PhoneNumber != null && p.PhoneNumber != "");
        if (hasPhone == false) query = query.Where(p => p.PhoneNumber == null || p.PhoneNumber == "");
        if (hasEmail == true) query = query.Where(p => p.Email != null && p.Email != "");
        if (hasEmail == false) query = query.Where(p => p.Email == null || p.Email == "");

        return query;
    }
}
