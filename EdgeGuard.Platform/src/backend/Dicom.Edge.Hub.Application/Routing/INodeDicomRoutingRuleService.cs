using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;

namespace Dicom.Edge.Hub.Application.Routing;

public interface INodeDicomRoutingRuleService
{
    Task<IReadOnlyList<NodeDicomRoutingRule>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default);
    Task<NodeDicomRoutingRule> CreateAsync(string nodeId, CreateNodeDicomRoutingRuleRequest request, CancellationToken ct = default);
    Task<NodeDicomRoutingRule?> UpdateAsync(string id, UpdateNodeDicomRoutingRuleRequest request, CancellationToken ct = default);
    Task<bool> EnableAsync(string id, CancellationToken ct = default);
    Task<bool> DisableAsync(string id, CancellationToken ct = default);
    Task<bool> UpdatePriorityAsync(string id, int priority, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
