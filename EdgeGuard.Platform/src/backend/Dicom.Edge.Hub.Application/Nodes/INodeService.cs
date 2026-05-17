using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;

namespace Dicom.Edge.Hub.Application.Nodes;

/// <summary>
/// Application service for Node aggregate write operations.
/// Encapsulates entity creation, state transitions, and persistence.
/// Read operations remain at the controller-repository level (CQRS-light).
/// </summary>
public interface INodeService
{
    /// <summary>Creates a new node from the request DTO and persists it.</summary>
    Task<Node> CreateAsync(CreateNodeRequest request, CancellationToken ct = default);

    /// <summary>Updates node metadata. Returns false if the node was not found.</summary>
    Task<bool> UpdateAsync(string id, UpdateNodeRequest request, CancellationToken ct = default);

    /// <summary>Enables a node. Returns false if the node was not found.</summary>
    Task<bool> EnableAsync(string id, CancellationToken ct = default);

    /// <summary>Disables a node. Returns false if the node was not found.</summary>
    Task<bool> DisableAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Assigns a PACS server to a node. Returns false if node or PACS was not found.
    /// Idempotent: if the assignment already exists and is active, it returns true.
    /// </summary>
    Task<bool> AssignPacsAsync(string nodeId, string pacsId, AssignPacsRequest request, CancellationToken ct = default);

    /// <summary>Removes the active assignment of a PACS server from a node. Returns false if not found.</summary>
    Task<bool> UnassignPacsAsync(string nodeId, string pacsId, CancellationToken ct = default);
}
