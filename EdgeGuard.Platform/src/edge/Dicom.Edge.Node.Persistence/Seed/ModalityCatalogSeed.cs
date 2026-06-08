using Dicom.Edge.Contracts.Configuration;

namespace Dicom.Edge.Node.Persistence.Seed;

/// <summary>
/// Idempotent seeder for the edge-local <c>modalities</c> reference table, driven by the
/// shared <see cref="ModalitySeed"/> (same source as the Hub). Inserts missing codes and
/// refreshes seed-owned fields on existing rows; the local <c>IsActive</c> flag is preserved.
/// </summary>
internal static class ModalityCatalogSeed
{
    public static async Task SeedAsync(EdgeNodeDbContext ctx, CancellationToken ct = default)
    {
        var existing = await ctx.Modalities.ToDictionaryAsync(m => m.Code, ct);
        var dirty = false;

        foreach (var entry in ModalitySeed.All)
        {
            if (existing.TryGetValue(entry.Code, out var modality))
            {
                if (modality.DisplayName != entry.DisplayName ||
                    modality.IsSupported != entry.IsSupported ||
                    modality.SortOrder   != entry.SortOrder)
                {
                    modality.DisplayName = entry.DisplayName;
                    modality.IsSupported = entry.IsSupported;
                    modality.SortOrder   = entry.SortOrder;
                    dirty = true;
                }
            }
            else
            {
                ctx.Modalities.Add(new ModalityCatalogEntry
                {
                    Code        = entry.Code,
                    DisplayName = entry.DisplayName,
                    IsSupported = entry.IsSupported,
                    IsActive    = true,
                    SortOrder   = entry.SortOrder,
                });
                dirty = true;
            }
        }

        if (dirty)
            await ctx.SaveChangesAsync(ct);
    }
}
