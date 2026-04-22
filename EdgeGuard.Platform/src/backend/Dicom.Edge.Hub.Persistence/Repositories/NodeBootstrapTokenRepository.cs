using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class NodeBootstrapTokenRepository(HubDbContext context) : INodeBootstrapTokenRepository
{
    public async Task<NodeBootstrapToken> AddAsync(NodeBootstrapToken token, CancellationToken ct = default)
    {
        await context.NodeBootstrapTokens.AddAsync(token, ct);
        return token;
    }

    public async Task<NodeBootstrapToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        await context.NodeBootstrapTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task UpdateAsync(NodeBootstrapToken token, CancellationToken ct = default)
    {
        context.NodeBootstrapTokens.Update(token);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<NodeBootstrapToken>> GetActiveAsync(CancellationToken ct = default) =>
        await context.NodeBootstrapTokens
            .AsNoTracking()
            .Where(t => !t.IsConsumed && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
}
