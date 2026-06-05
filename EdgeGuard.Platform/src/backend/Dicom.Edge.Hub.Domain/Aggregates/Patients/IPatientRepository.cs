using Dicom.Edge.Common.Filters;
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

    /// <summary>Returns a paged, filtered, and sorted list of patients.</summary>
    Task<PagedResult<Patient>> GetFilteredPagedAsync(
        PaginationRequest pagination,
        PatientFilterCriteria filter,
        CancellationToken ct = default);

    /// <summary>Returns all patients matching filters (no pagination) for export.</summary>
    Task<IReadOnlyList<Patient>> GetFilteredAllAsync(
        PatientFilterCriteria filter,
        CancellationToken ct = default);

    Task<Patient> AddAsync(Patient patient, CancellationToken ct = default);
    Task UpdateAsync(Patient patient, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// P0-7: Returns all patients whose <c>MergedIntoPatientId</c> points to the given
    /// prior PatientDicomId. Used to collapse merge chains when the surviving patient
    /// is itself merged later (A→B then B→C must re-point A to C).
    /// </summary>
    Task<IReadOnlyList<Patient>> GetByMergedIntoPatientIdAsync(
        string priorPatientDicomId,
        CancellationToken ct = default);
}
