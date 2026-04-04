using Dicom.Edge.Common.Pagination;

namespace Dicom.Edge.Hub.Domain.Aggregates.Patients;

/// <summary>
/// Repository interface for Patient aggregate.
/// </summary>
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Patient?> GetByPatientDicomIdAsync(string patientDicomId, CancellationToken ct = default);
    Task<IReadOnlyList<Patient>> FindByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Patient>> GetByNodeAsync(string nodeId, CancellationToken ct = default);
    Task<IReadOnlyList<Patient>> GetActiveAsync(CancellationToken ct = default);
    Task<PagedResult<Patient>> GetPagedAsync(PaginationRequest pagination, CancellationToken ct = default);
    Task<Patient> AddAsync(Patient patient, CancellationToken ct = default);
    Task UpdateAsync(Patient patient, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
