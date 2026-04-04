using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Infrastructure.Repositories;

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
            .Where(p => p.PatientName.Contains(name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Patient>> GetByNodeAsync(string nodeId, CancellationToken ct = default) =>
        await _context.Patients
            .Where(p => p.CreatedByNodeId == nodeId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Patient>> GetActiveAsync(CancellationToken ct = default) =>
        await _context.Patients
            .Where(p => p.IsActive)
            .ToListAsync(ct);

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
