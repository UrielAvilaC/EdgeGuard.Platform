namespace Dicom.Edge.Hub.Domain.Services;

/// <summary>
/// Domain service for inheriting global PACS assignments to nodes.
/// </summary>
public interface IPacsInheritanceService
{
    /// <summary>
    /// Assigns all global PACS to the given node.
    /// </summary>
    Task InheritGlobalPacsToNodeAsync(string nodeId, CancellationToken ct = default);

    /// <summary>
    /// When a new global PACS is created, propagate to all active nodes.
    /// </summary>
    Task PropagateGlobalPacsToAllNodesAsync(string pacsId, CancellationToken ct = default);
}
