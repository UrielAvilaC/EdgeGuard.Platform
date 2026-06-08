using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Seed;

/// <summary>
/// Idempotent seeder for the <c>outbox_topics</c> catalog. Runs on every startup and
/// inserts only the well-known topics that are not yet present (safe for upgrades).
/// </summary>
public static class OutboxTopicSeed
{
    public static async Task SeedMissingAsync(HubDbContext ctx, CancellationToken ct = default)
    {
        var existing = await ctx.OutboxTopics
            .Select(t => t.Id)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var missing = OutboxTopicCatalog.Build()
            .Where(t => !existingSet.Contains(t.Id))
            .ToList();

        if (missing.Count == 0) return;

        await ctx.OutboxTopics.AddRangeAsync(missing, ct);
        await ctx.SaveChangesAsync(ct);
    }
}
