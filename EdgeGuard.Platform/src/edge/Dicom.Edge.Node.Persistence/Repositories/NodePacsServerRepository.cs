using Dicom.Edge.Node.Persistence.Context;
using Dicom.Edge.Node.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Node.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="INodePacsServerRepository"/>.
/// Uses a dedicated <see cref="EdgeNodeDbContext"/> created from the factory
/// so it is safe to call from singletons and background services.
/// </summary>
public sealed class NodePacsServerRepository(IDbContextFactory<EdgeNodeDbContext> factory)
    : INodePacsServerRepository
{
    public async Task<IReadOnlyList<NodePacsServer>> GetAllEnabledAsync(CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);
        return await ctx.NodePacsServers
            .AsNoTracking()
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NodePacsServer>> GetAllAsync(CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);
        return await ctx.NodePacsServers
            .AsNoTracking()
            .OrderBy(p => p.Priority)
            .ToListAsync(ct);
    }

    public async Task<NodePacsServer?> GetByAeTitleAsync(string aeTitle, CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);
        return await ctx.NodePacsServers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AeTitle == aeTitle, ct);
    }

    public async Task SyncFromHubAsync(IReadOnlyList<NodePacsServer> servers, CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);

        var incomingIds = servers.Select(s => s.Id).ToHashSet();

        // Remove PACS no longer assigned in the Hub
        var stale = await ctx.NodePacsServers
            .Where(p => !incomingIds.Contains(p.Id))
            .ToListAsync(ct);

        if (stale.Count > 0)
            ctx.NodePacsServers.RemoveRange(stale);

        // Upsert each server
        foreach (var server in servers)
        {
            var existing = await ctx.NodePacsServers.FindAsync([server.Id], ct);
            if (existing is null)
            {
                server.SyncedAt = DateTime.UtcNow;
                await ctx.NodePacsServers.AddAsync(server, ct);
            }
            else
            {
                existing.Name      = server.Name;
                existing.AeTitle   = server.AeTitle;
                existing.Host      = server.Host;
                existing.Port      = server.Port;
                existing.Priority  = server.Priority;
                existing.IsEnabled = server.IsEnabled;
                existing.SyncedAt  = DateTime.UtcNow;
            }
        }

        await ctx.SaveChangesAsync(ct);
    }
}
