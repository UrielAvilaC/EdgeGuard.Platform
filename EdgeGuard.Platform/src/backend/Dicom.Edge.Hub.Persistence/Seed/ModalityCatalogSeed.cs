using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.Modalities;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Seed;

/// <summary>
/// Idempotent seeder for the <c>modalities</c> reference catalog, driven by the shared
/// <see cref="ModalitySeed"/>. Inserts missing codes and refreshes seed-owned fields
/// (display name, IsSupported, sort order) on existing rows. The operator-controlled
/// <c>IsActive</c> flag is preserved across re-seeds.
/// </summary>
public static class ModalityCatalogSeed
{
    public static async Task SeedAsync(HubDbContext ctx, CancellationToken ct = default)
    {
        var existing = await ctx.Modalities.ToDictionaryAsync(m => m.Code, ct);
        var dirty = false;

        foreach (var entry in ModalitySeed.All)
        {
            if (existing.TryGetValue(entry.Code, out var modality))
            {
                modality.UpdateFromSeed(entry.DisplayName, entry.IsSupported, entry.SortOrder);
                dirty = true;
            }
            else
            {
                await ctx.Modalities.AddAsync(
                    Modality.Create(entry.Code, entry.DisplayName, entry.IsSupported, entry.SortOrder), ct);
                dirty = true;
            }
        }

        if (dirty)
            await ctx.SaveChangesAsync(ct);
    }
}
