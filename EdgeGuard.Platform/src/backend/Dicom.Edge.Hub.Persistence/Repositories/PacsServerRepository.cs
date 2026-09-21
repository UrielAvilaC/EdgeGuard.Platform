using System.Linq.Expressions;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Common.Sorting;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class PacsServerRepository : IPacsServerRepository
{
    private readonly HubDbContext _context;

    public PacsServerRepository(HubDbContext context) => _context = context;

    public async Task<PacsServer?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.PacsServers.FindAsync([id], ct);

    public async Task<PacsServer?> GetByAeTitleAsync(string aeTitle, CancellationToken ct = default) =>
        await _context.PacsServers
            .FirstOrDefaultAsync(p => p.AeTitle.Value == aeTitle, ct);

    public async Task<IReadOnlyList<PacsServer>> GetAllAsync(CancellationToken ct = default) =>
        await _context.PacsServers.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<PacsServer>> GetGlobalAsync(CancellationToken ct = default) =>
        await _context.PacsServers
            .AsNoTracking()
            .Where(p => p.IsGlobal)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PacsServer>> GetEnabledAsync(CancellationToken ct = default) =>
        await _context.PacsServers
            .AsNoTracking()
            .Where(p => p.IsEnabled)
            .ToListAsync(ct);

    public async Task<PacsServer> AddAsync(PacsServer pacs, CancellationToken ct = default)
    {
        await _context.PacsServers.AddAsync(pacs, ct);
        return pacs;
    }

    public Task UpdateAsync(PacsServer pacs, CancellationToken ct = default)
    {
        _context.PacsServers.Update(pacs);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Borrado lógico. Antes hacía <c>Remove</c>, y eso dejaba los estudios históricos
    /// apuntando con <c>TargetPacsId</c> a una fila inexistente — sin error, porque no es
    /// clave foránea, pero perdiendo el rastro de a dónde se había entregado cada estudio.
    /// </summary>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        // Lectura rastreada (el filtro global excluye los ya eliminados, así que repetir
        // la operación sobre uno borrado simplemente no encuentra nada).
        var pacs = await _context.PacsServers.FirstOrDefaultAsync(p => p.Id == id, ct);
        pacs?.SoftDelete();
    }

    public async Task<PagedResult<PacsServer>> GetFilteredPagedAsync(PaginationRequest pagination, PacsServerFilterCriteria filter, CancellationToken ct = default)
    {
        var query = _context.PacsServers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p => p.Name.Contains(filter.Search) || p.AeTitle.Value.Contains(filter.Search) || p.HostName.Contains(filter.Search));
        if (filter.IsEnabled.HasValue) query = query.Where(p => p.IsEnabled == filter.IsEnabled.Value);
        if (filter.IsGlobal.HasValue) query = query.Where(p => p.IsGlobal == filter.IsGlobal.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .ApplySort(filter.SortBy, filter.SortDir, PacsSortFields, q => q.OrderBy(p => p.Name))
            .Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct);

        return new PagedResult<PacsServer> { Items = items, Page = pagination.Page, PageSize = pagination.PageSize, TotalCount = totalCount };
    }

    private static readonly Dictionary<string, Expression<Func<PacsServer, object?>>> PacsSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = p => p.Name,
        ["aeTitle"] = p => p.AeTitle.Value,
        ["hostName"] = p => p.HostName,
        ["port"] = p => p.Port,
        ["isEnabled"] = p => p.IsEnabled,
        ["isGlobal"] = p => p.IsGlobal,
        ["createdAt"] = p => p.CreatedAt,
        // Sin "lastCEchoAt": el PACS ya no guarda conectividad, y ordenar un catálogo
        // global por un sondeo que es de cada nodo no tendría un valor que mirar.
    };
}
