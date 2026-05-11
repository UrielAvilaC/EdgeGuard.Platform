using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;

namespace Dicom.Edge.Hub.Domain.Aggregates.Pacs;

/// <summary>
/// Repository interface for PacsServer aggregate.
/// </summary>
public interface IPacsServerRepository
{
    Task<PacsServer?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PacsServer?> GetByAeTitleAsync(string aeTitle, CancellationToken ct = default);
    Task<IReadOnlyList<PacsServer>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PacsServer>> GetGlobalAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PacsServer>> GetEnabledAsync(CancellationToken ct = default);
    Task<PagedResult<PacsServer>> GetFilteredPagedAsync(PaginationRequest pagination, PacsServerFilterCriteria filter, CancellationToken ct = default);
    Task<PacsServer> AddAsync(PacsServer pacs, CancellationToken ct = default);
    Task UpdateAsync(PacsServer pacs, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
