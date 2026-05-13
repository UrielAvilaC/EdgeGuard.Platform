using Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class NodeConfigurationProfileRepository : INodeConfigurationProfileRepository
{
    private readonly HubDbContext _context;

    public NodeConfigurationProfileRepository(HubDbContext context) => _context = context;

    public async Task<IReadOnlyList<NodeConfigurationProfile>> GetByNodeIdAsync(
        string nodeId, CancellationToken ct = default) =>
        await _context.NodeConfigurationProfiles
            .Where(p => p.NodeId == nodeId)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SettingKey)
            .ToListAsync(ct);

    public async Task<NodeConfigurationProfile?> GetByNodeAndKeyAsync(
        string nodeId, string settingKey, CancellationToken ct = default) =>
        await _context.NodeConfigurationProfiles
            .FindAsync([nodeId, settingKey], ct);

    public async Task<IReadOnlyList<NodeConfigurationProfile>> GetByNodeAndCategoryAsync(
        string nodeId, string category, CancellationToken ct = default) =>
        await _context.NodeConfigurationProfiles
            .Where(p => p.NodeId == nodeId && p.Category == category)
            .OrderBy(p => p.SettingKey)
            .ToListAsync(ct);

    public async Task AddRangeAsync(
        IEnumerable<NodeConfigurationProfile> profiles, CancellationToken ct = default) =>
        await _context.NodeConfigurationProfiles.AddRangeAsync(profiles, ct);

    public async Task<bool> ExistsForNodeAsync(string nodeId, CancellationToken ct = default) =>
        await _context.NodeConfigurationProfiles.AnyAsync(p => p.NodeId == nodeId, ct);
}
