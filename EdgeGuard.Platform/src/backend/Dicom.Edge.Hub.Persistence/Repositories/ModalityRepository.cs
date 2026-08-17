using Dicom.Edge.Hub.Domain.Aggregates.Modalities;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class ModalityRepository(HubDbContext context) : IModalityRepository
{
    public async Task<IReadOnlyList<Modality>> GetAllAsync(
        bool supportedOnly = false, CancellationToken ct = default)
    {
        var query = context.Modalities.AsNoTracking().AsQueryable();
        if (supportedOnly)
            query = query.Where(m => m.IsSupported && m.IsActive);

        return await query.OrderBy(m => m.SortOrder).ToListAsync(ct);
    }

    public async Task<Modality?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await context.Modalities.FindAsync([normalized], ct);
    }

    public async Task<IReadOnlyList<Modality>> GetByCodesAsync(
        IEnumerable<string> codes, CancellationToken ct = default)
    {
        var set = codes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        if (set.Count == 0) return [];

        return await context.Modalities
            .AsNoTracking()
            .Where(m => set.Contains(m.Code))
            .ToListAsync(ct);
    }
}
