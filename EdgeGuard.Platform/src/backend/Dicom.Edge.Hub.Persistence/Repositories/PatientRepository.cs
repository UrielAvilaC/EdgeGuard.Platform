using Dicom.Edge.Common.Pagination;
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
        return patient;
    }

    public Task UpdateAsync(Patient patient, CancellationToken ct = default)
    {
        _context.Patients.Update(patient);
        return Task.CompletedTask;
    }

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _context.Patients.CountAsync(ct);
}
