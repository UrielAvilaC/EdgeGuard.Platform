using Dicom.Edge.Hub.Domain.Aggregates.Configuration;

namespace Dicom.Edge.Hub.Application.Configuration;

public sealed record SystemSettingDto(
    string Key,
    string Value,
    string Category,
    string DisplayName,
    string ValueType,
    string? Description,
    bool IsReadOnly);

public interface ISystemSettingsService
{
    Task<SystemSettingDto?> GetAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSettingDto>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
    Task SeedDefaultsAsync(CancellationToken ct = default);
}
