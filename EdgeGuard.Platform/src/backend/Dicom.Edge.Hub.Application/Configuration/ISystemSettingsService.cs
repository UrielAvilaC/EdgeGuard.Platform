using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Hub.Application.Configuration;

public interface ISystemSettingsService
{
    Task<SystemSettingDto?> GetAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSettingDto>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
    Task SeedDefaultsAsync(CancellationToken ct = default);
}
