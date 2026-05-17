namespace Dicom.Edge.Hub.Domain.Aggregates.Routing;

public interface INodeDicomRoutingRuleRepository
{
    Task<IReadOnlyList<NodeDicomRoutingRule>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default);
    Task<NodeDicomRoutingRule?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(NodeDicomRoutingRule rule, CancellationToken ct = default);
    Task UpdateAsync(NodeDicomRoutingRule rule, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
