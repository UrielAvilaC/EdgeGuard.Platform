using System.Linq.Expressions;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Common.Sorting;
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

    public async Task<PagedResult<Hl7RoutingRule>> GetFilteredPagedAsync(PaginationRequest pagination, RoutingRuleFilterCriteria filter, CancellationToken ct = default)
    {
        var query = _context.Hl7RoutingRules.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(r => r.Name.Contains(filter.Search));
        if (filter.IsEnabled.HasValue) query = query.Where(r => r.IsEnabled == filter.IsEnabled.Value);
        if (!string.IsNullOrWhiteSpace(filter.TargetNodeId)) query = query.Where(r => r.TargetNodeId == filter.TargetNodeId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .ApplySort(filter.SortBy, filter.SortDir, RuleSortFields, q => q.OrderBy(r => r.Priority))
            .Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct);

        return new PagedResult<Hl7RoutingRule> { Items = items, Page = pagination.Page, PageSize = pagination.PageSize, TotalCount = totalCount };
    }

    private static readonly Dictionary<string, Expression<Func<Hl7RoutingRule, object?>>> RuleSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = r => r.Name,
        ["priority"] = r => r.Priority,
        ["isEnabled"] = r => r.IsEnabled,
        ["targetNodeId"] = r => r.TargetNodeId,
        ["matchCount"] = r => r.MatchCount,
        ["lastMatchedAt"] = r => r.LastMatchedAt,
        ["createdAt"] = r => r.CreatedAt,
    };
}
