using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies;

/// <summary>
/// Repository interface for Study aggregate.
/// </summary>
public interface IStudyRepository
{
    Task<Study?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Study?> GetByStudyInstanceUidAsync(string studyInstanceUid, CancellationToken ct = default);
    Task<Study?> GetByAccessionNumberAsync(string accessionNumber, CancellationToken ct = default);
    /// <summary>
    /// Studies whose DICOM Patient ID (MRN) matches. Used by the HL7 merge flows, which
    /// key on the MRN. To list a patient's studies use <see cref="GetByPatientAsync"/>.
    /// </summary>
    Task<IReadOnlyList<Study>> GetByPatientIdAsync(string patientId, CancellationToken ct = default);

    /// <summary>
    /// All studies belonging to a patient: those linked by FK plus, when
    /// <paramref name="patientDicomId"/> is given, any legacy row still matched only by MRN.
    /// </summary>
    Task<IReadOnlyList<Study>> GetByPatientAsync(
        string patientRecordId,
        string? patientDicomId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Studies carrying the given MRN but no patient FK yet. Used to attach the link when
    /// the patient is registered after the studies arrived.
    /// </summary>
    Task<IReadOnlyList<Study>> GetUnlinkedByPatientIdAsync(string patientId, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetByNodeAsync(string nodeId, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetByStatusAsync(StudyStatus status, CancellationToken ct = default);

    /// <summary>Studies at a given point of the PACS-send pipeline (independent of clinical status).</summary>
    Task<IReadOnlyList<Study>> GetByPacsStatusAsync(StudyPacsStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetPendingForPacsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Study>> GetStudiesForCleanupAsync(string modality, DateTime olderThan, CancellationToken ct = default);
    Task<PagedResult<Study>> GetPagedAsync(PaginationRequest pagination, CancellationToken ct = default);

    /// <summary>Returns a paged, filtered, and sorted list of studies.</summary>
    Task<PagedResult<Study>> GetFilteredPagedAsync(
        PaginationRequest pagination,
        StudyFilterCriteria filter,
        CancellationToken ct = default);

    /// <summary>Returns all studies matching filters (no pagination) for export.</summary>
    Task<IReadOnlyList<Study>> GetFilteredAllAsync(
        StudyFilterCriteria filter,
        CancellationToken ct = default);

    Task<Study> AddAsync(Study study, CancellationToken ct = default);
    Task UpdateAsync(Study study, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
