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

    /// <summary>
    /// Todos los nodos, sin rastreo. Para listar y proyectar; no para escribir después.
    /// </summary>
    Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Todos los nodos, rastreados, para los caminos de lectura-modificación-escritura.
    /// <para>La diferencia con <see cref="GetAllAsync"/> no es una optimización: el
    /// snapshot que deja el rastreo es lo único que preserva el <c>UpdatedAt</c> original,
    /// que es el token de concurrencia de <see cref="Node"/>. Escribir sobre entidades sin
    /// rastrear produce un <c>UPDATE</c> que no afecta ninguna fila.</para>
    /// </summary>
    Task<IReadOnlyList<Node>> GetAllForUpdateAsync(CancellationToken ct = default);

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
