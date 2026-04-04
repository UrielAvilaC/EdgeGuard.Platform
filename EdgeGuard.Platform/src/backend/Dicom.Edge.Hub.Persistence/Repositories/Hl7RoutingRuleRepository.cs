using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class Hl7RoutingRuleRepository : IHl7RoutingRuleRepository
{
    private readonly HubDbContext _context;

    public Hl7RoutingRuleRepository(HubDbContext context) => _context = context;

    public async Task<Hl7RoutingRule?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Hl7RoutingRules.FindAsync([id], ct);

    public async Task<IReadOnlyList<Hl7RoutingRule>> GetEnabledOrderedAsync(CancellationToken ct = default) =>
        await _context.Hl7RoutingRules
            .AsNoTracking()
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Hl7RoutingRule>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Hl7RoutingRules
            .AsNoTracking()
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

    public async Task<Hl7RoutingRule> AddAsync(Hl7RoutingRule rule, CancellationToken ct = default)
    {
        await _context.Hl7RoutingRules.AddAsync(rule, ct);
        return rule;
    }

    public Task UpdateAsync(Hl7RoutingRule rule, CancellationToken ct = default)
    {
        var entry = _context.Entry(rule);

        if (entry.State == EntityState.Detached)
            _context.Hl7RoutingRules.Update(rule);

        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var rule = await _context.Hl7RoutingRules.FindAsync([id], ct);
        if (rule is not null)
            _context.Hl7RoutingRules.Remove(rule);
    }
}
