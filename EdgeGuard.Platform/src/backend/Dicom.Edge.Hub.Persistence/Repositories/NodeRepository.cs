using System.Linq.Expressions;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Common.Sorting;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class NodeRepository : INodeRepository
{
    private readonly HubDbContext _context;

    public NodeRepository(HubDbContext context) => _context = context;

    public async Task<Node?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Nodes.FindAsync([id], ct);

    public async Task<Node?> GetByNameAndIpAsync(string name, string ipAddress, CancellationToken ct = default) =>
        await _context.Nodes
            .FirstOrDefaultAsync(n => n.Name == name && n.IpAddress == ipAddress, ct);

    public async Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Nodes.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Node>> GetActiveNodesAsync(CancellationToken ct = default) =>
        await _context.Nodes
            .AsNoTracking()
            .Where(n => n.IsEnabled && n.Status != NodeStatus.Offline)
            .ToListAsync(ct);

    public async Task<Node?> GetWithPacsAssignmentsAsync(string id, CancellationToken ct = default) =>
        await _context.Nodes
            .Include(n => n.PacsAssignments)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<PagedResult<Node>> GetPagedAsync(PaginationRequest pagination, CancellationToken ct = default)
    {
        var query = _context.Nodes.AsNoTracking().OrderBy(n => n.Name);
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct);

        return new PagedResult<Node>
        {
            Items = items,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Node> AddAsync(Node node, CancellationToken ct = default)
    {
        await _context.Nodes.AddAsync(node, ct);
        return node;
    }

    public async Task<IReadOnlyList<Node>> GetNodesWithApiKeyAsync(CancellationToken ct = default) =>
        await _context.Nodes
            .AsNoTracking()
            .Where(n => n.ApiKeyHash != null && !n.IsDeleted)
            .ToListAsync(ct);

    public Task UpdateAsync(Node node, CancellationToken ct = default)
    {
        _context.Nodes.Update(node);
        return Task.CompletedTask;
    }

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _context.Nodes.CountAsync(ct);

    public async Task<PagedResult<Node>> GetFilteredPagedAsync(PaginationRequest pagination, NodeFilterCriteria filter, CancellationToken ct = default)
    {
        var query = _context.Nodes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(n => n.Name.Contains(filter.Search) || n.AeTitle.Value.Contains(filter.Search) || n.IpAddress.Contains(filter.Search));
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<NodeStatus>(filter.Status, true, out var status))
            query = query.Where(n => n.Status == status);
        if (filter.IsEnabled.HasValue)
            query = query.Where(n => n.IsEnabled == filter.IsEnabled.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .ApplySort(filter.SortBy, filter.SortDir, NodeSortFields, q => q.OrderBy(n => n.Name))
            .Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct);

        return new PagedResult<Node> { Items = items, Page = pagination.Page, PageSize = pagination.PageSize, TotalCount = totalCount };
    }

    private static readonly Dictionary<string, Expression<Func<Node, object?>>> NodeSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = n => n.Name,
        ["aeTitle"] = n => n.AeTitle.Value,
        ["status"] = n => n.Status,
        ["createdAt"] = n => n.CreatedAt,
        ["lastHeartbeatAt"] = n => n.LastHeartbeatAt,
        ["ipAddress"] = n => n.IpAddress,
        ["isEnabled"] = n => n.IsEnabled,
        ["availableStorageMb"] = n => n.AvailableStorageMb,
    };
}
