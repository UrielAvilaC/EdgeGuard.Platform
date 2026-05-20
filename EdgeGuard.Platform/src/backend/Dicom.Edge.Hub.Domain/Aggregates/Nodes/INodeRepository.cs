using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes;

/// <summary>
/// Repository interface for Node aggregate.
/// </summary>
public interface INodeRepository
{
    Task<Node?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Looks up a node by Name + IP for re-registration discovery.</summary>
    Task<Node?> GetByNameAndIpAsync(string name, string ipAddress, CancellationToken ct = default);

    Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Node>> GetActiveNodesAsync(CancellationToken ct = default);
    Task<Node?> GetWithPacsAssignmentsAsync(string id, CancellationToken ct = default);
    Task<PagedResult<Node>> GetPagedAsync(PaginationRequest pagination, CancellationToken ct = default);
    Task<PagedResult<Node>> GetFilteredPagedAsync(PaginationRequest pagination, NodeFilterCriteria filter, CancellationToken ct = default);
    /// <summary>Returns all nodes that have an API key assigned (for M2M auth lookup).</summary>
    Task<IReadOnlyList<Node>> GetNodesWithApiKeyAsync(CancellationToken ct = default);
    Task<Node> AddAsync(Node node, CancellationToken ct = default);
    Task UpdateAsync(Node node, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
