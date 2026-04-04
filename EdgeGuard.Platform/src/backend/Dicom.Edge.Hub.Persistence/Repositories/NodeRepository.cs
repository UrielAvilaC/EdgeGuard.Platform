using Dicom.Edge.Common.Pagination;
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

    public async Task<Node?> GetByAeTitleAsync(string aeTitle, CancellationToken ct = default) =>
        await _context.Nodes
            .FirstOrDefaultAsync(n => n.AeTitle.Value == aeTitle, ct);

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

    public Task UpdateAsync(Node node, CancellationToken ct = default)
    {
        _context.Nodes.Update(node);
        return Task.CompletedTask;
    }

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _context.Nodes.CountAsync(ct);
}
