using Dicom.Edge.Common.Pagination;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes;

/// <summary>
/// Repository interface for Node aggregate.
/// </summary>
public interface INodeRepository
{
    Task<Node?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Node?> GetByAeTitleAsync(string aeTitle, CancellationToken ct = default);
    Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Node>> GetActiveNodesAsync(CancellationToken ct = default);
    Task<Node?> GetWithPacsAssignmentsAsync(string id, CancellationToken ct = default);
    Task<PagedResult<Node>> GetPagedAsync(PaginationRequest pagination, CancellationToken ct = default);
    Task<Node> AddAsync(Node node, CancellationToken ct = default);
    Task UpdateAsync(Node node, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
