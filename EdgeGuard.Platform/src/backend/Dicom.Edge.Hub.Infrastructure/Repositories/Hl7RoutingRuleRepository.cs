using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Infrastructure.Repositories;

public sealed class Hl7RoutingRuleRepository : IHl7RoutingRuleRepository
{
    private readonly HubDbContext _context;

    public Hl7RoutingRuleRepository(HubDbContext context) => _context = context;

    public async Task<Hl7RoutingRule?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Hl7RoutingRules.FindAsync([id], ct);

    public async Task<IReadOnlyList<Hl7RoutingRule>> GetEnabledOrderedAsync(CancellationToken ct = default) =>
        await _context.Hl7RoutingRules
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Hl7RoutingRule>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Hl7RoutingRules
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

    public async Task<Hl7RoutingRule> AddAsync(Hl7RoutingRule rule, CancellationToken ct = default)
    {
        await _context.Hl7RoutingRules.AddAsync(rule, ct);
        return rule;
    }

    public async Task UpdateAsync(Hl7RoutingRule rule, CancellationToken ct = default)
    {
        var entry = _context.Entry(rule);

        if (entry.State == EntityState.Detached)
            _context.Hl7RoutingRules.Update(rule);

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var rule = await _context.Hl7RoutingRules.FindAsync([id], ct);
        if (rule is not null)
            _context.Hl7RoutingRules.Remove(rule);
    }
}
