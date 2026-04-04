using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class SystemSettingRepository : ISystemSettingRepository
{
    private readonly HubDbContext _context;

    public SystemSettingRepository(HubDbContext context) => _context = context;

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        await _context.SystemSettings.FindAsync([key.ToLowerInvariant()], ct);

    public async Task<IReadOnlyList<SystemSetting>> GetByCategoryAsync(string category, CancellationToken ct = default) =>
        await _context.SystemSettings
            .Where(s => s.Category == category)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken ct = default) =>
        await _context.SystemSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<SystemSetting> AddAsync(SystemSetting setting, CancellationToken ct = default)
    {
        await _context.SystemSettings.AddAsync(setting, ct);
        return setting;
    }

    public async Task UpdateAsync(SystemSetting setting, CancellationToken ct = default)
    {
        var entry = _context.Entry(setting);

        if (entry.State == EntityState.Detached)
            _context.SystemSettings.Update(setting);

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var setting = await _context.SystemSettings.FindAsync([key.ToLowerInvariant()], ct);
        if (setting is not null)
            _context.SystemSettings.Remove(setting);
    }
}
