namespace Dicom.Edge.Hub.Domain.Aggregates.Pacs;

/// <summary>
/// Repository interface for PACS send audit records.
/// </summary>
public interface IPacsSendAuditRepository
{
    Task<PacsSendAudit> AddAsync(PacsSendAudit audit, CancellationToken ct = default);
    Task<IReadOnlyList<PacsSendAudit>> GetByStudyAsync(string studyId, CancellationToken ct = default);
    Task<IReadOnlyList<PacsSendAudit>> GetByPacsAsync(string pacsId, int limit = 100, CancellationToken ct = default);
    Task<PacsSendAudit?> GetLatestByStudyAsync(string studyId, CancellationToken ct = default);

    /// <summary>
    /// Deletes audit records older than the specified cutoff date in batches. Returns the count deleted.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default);
}
