using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Seed;

/// <summary>
/// Idempotent seeder for the <c>system_settings</c> table.
/// <list type="bullet">
///   <item><see cref="SeedMissingAsync"/> — inserts only keys absent from the DB (safe for upgrades).</item>
///   <item><see cref="SeedIfEmptyAsync"/> — inserts all defaults only if the table is completely empty.</item>
/// </list>
/// </summary>
public static class HubSettingsSeed
{
    /// <summary>
    /// Called on every startup. Inserts any keys that were added in newer versions
    /// without touching keys that already exist (safe for upgrades).
    /// </summary>
    public static async Task SeedMissingAsync(HubDbContext ctx, CancellationToken ct = default)
    {
        var existing = await ctx.SystemSettings
            .Select(s => s.Id)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var missing = BuildDefaults()
            .Where(s => !existingSet.Contains(s.Id))
            .ToList();

        if (missing.Count == 0) return;

        await ctx.SystemSettings.AddRangeAsync(missing, ct);
        await ctx.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Inserts all defaults only if the table is completely empty.
    /// Useful for first-run initialization.
    /// </summary>
    public static async Task SeedIfEmptyAsync(HubDbContext ctx, CancellationToken ct = default)
    {
        if (await ctx.SystemSettings.AnyAsync(ct)) return;

        await ctx.SystemSettings.AddRangeAsync(BuildDefaults(), ct);
        await ctx.SaveChangesAsync(ct);
    }

    // ── Seed definitions ──────────────────────────────────────────────────────
    // Single source of truth lives in the Domain (HubSettingsDefaults) so this
    // startup seeder and SystemSettingsService.SeedDefaultsAsync never diverge.

    private static IReadOnlyList<SystemSetting> BuildDefaults() => HubSettingsDefaults.Build();
}
