using Dicom.Edge.Hub.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dicom.Edge.Hub.Persistence.Interceptors;

/// <summary>
/// Automatically sets CreatedAt/UpdatedAt timestamps on entities.
/// Applied globally via DbContext interceptor registration.
/// </summary>
public class TimestampInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTime.UtcNow;
        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }

            if (entry.State == EntityState.Added &&
                entry.Metadata.FindProperty("CreatedAt") is not null)
            {
                var current = (DateTime?)entry.Property("CreatedAt").CurrentValue;
                if (!current.HasValue || current.Value == default)
                {
                    entry.Property("CreatedAt").CurrentValue = now;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
