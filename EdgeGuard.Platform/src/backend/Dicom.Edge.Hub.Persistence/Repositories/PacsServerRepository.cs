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

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var pacs = await _context.PacsServers.FindAsync([id], ct);
        if (pacs is not null)
            _context.PacsServers.Remove(pacs);
    }
}
