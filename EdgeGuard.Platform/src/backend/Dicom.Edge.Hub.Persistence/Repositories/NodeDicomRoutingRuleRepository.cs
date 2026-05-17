using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class NodeDicomRoutingRuleRepository(HubDbContext context) : INodeDicomRoutingRuleRepository
{
    public async Task<IReadOnlyList<NodeDicomRoutingRule>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default) =>
        await context.NodeDicomRoutingRules
            .AsNoTracking()
            .Where(r => r.NodeId == nodeId)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

    public async Task<NodeDicomRoutingRule?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await context.NodeDicomRoutingRules.FindAsync([id], ct);

    public async Task AddAsync(NodeDicomRoutingRule rule, CancellationToken ct = default) =>
        await context.NodeDicomRoutingRules.AddAsync(rule, ct);

    public Task UpdateAsync(NodeDicomRoutingRule rule, CancellationToken ct = default)
    {
        if (context.Entry(rule).State == EntityState.Detached)
            context.NodeDicomRoutingRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var rule = await context.NodeDicomRoutingRules.FindAsync([id], ct);
        if (rule is not null)
            context.NodeDicomRoutingRules.Remove(rule);
    }
}
