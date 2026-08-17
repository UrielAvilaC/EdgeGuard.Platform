using Dicom.Edge.Hub.Domain.Aggregates.Equipment;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class NodeEquipmentRepository(HubDbContext context) : INodeEquipmentRepository
{
    public async Task<IReadOnlyList<NodeEquipment>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default) =>
        await context.NodeEquipment
            .AsNoTracking()
            .Include(e => e.Modalities)
            .Where(e => e.NodeId == nodeId)
            .OrderBy(e => e.AeTitle)
            .ToListAsync(ct);

    public async Task<NodeEquipment?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await context.NodeEquipment
            .Include(e => e.Modalities)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<NodeEquipment?> GetByNodeAndAeTitleAsync(
        string nodeId, string aeTitle, CancellationToken ct = default)
    {
        var normalized = aeTitle.Trim().ToUpperInvariant();
        return await context.NodeEquipment
            .Include(e => e.Modalities)
            .FirstOrDefaultAsync(e => e.NodeId == nodeId && e.AeTitle == normalized, ct);
    }

    public async Task AddAsync(NodeEquipment equipment, CancellationToken ct = default) =>
        await context.NodeEquipment.AddAsync(equipment, ct);

    public Task UpdateAsync(NodeEquipment equipment, CancellationToken ct = default)
    {
        if (context.Entry(equipment).State == EntityState.Detached)
            context.NodeEquipment.Update(equipment);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var equipment = await context.NodeEquipment.FindAsync([id], ct);
        if (equipment is not null)
            context.NodeEquipment.Remove(equipment);
    }
}
