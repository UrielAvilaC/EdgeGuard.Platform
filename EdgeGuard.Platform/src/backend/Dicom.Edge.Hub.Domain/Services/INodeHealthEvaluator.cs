namespace Dicom.Edge.Hub.Domain.Services;

/// <summary>
/// Domain service for evaluating node health based on healthchecks.
/// </summary>
public interface INodeHealthEvaluator
{
    /// <summary>
    /// Evaluates the health of all nodes and updates their status if needed
    /// (e.g., mark as Offline if heartbeat interval exceeded).
    /// </summary>
    Task EvaluateAllNodesAsync(CancellationToken ct = default);

    /// <summary>
    /// Evaluates a single node's health.
    /// </summary>
    Task EvaluateNodeAsync(string nodeId, CancellationToken ct = default);
}
