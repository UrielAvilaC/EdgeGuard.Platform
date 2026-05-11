namespace Dicom.Edge.Hub.Domain.Aggregates.Configuration;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSetting>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken ct = default);
    Task<SystemSetting> AddAsync(SystemSetting setting, CancellationToken ct = default);
    Task UpdateAsync(SystemSetting setting, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
