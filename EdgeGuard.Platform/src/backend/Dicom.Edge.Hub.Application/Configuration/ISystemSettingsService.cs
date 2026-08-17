using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Hub.Application.Configuration;

public interface ISystemSettingsService
{
    Task<SystemSettingDto?> GetAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSettingDto>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken ct = default);
    /// <summary>
    /// Persists a setting value. Returns <c>false</c> when <paramref name="key"/> is not a
    /// known setting, so the caller can answer with an error instead of a false success.
    /// A known key whose row is missing (added in a newer version, DB not re-seeded yet) is
    /// inserted from its default definition rather than silently dropped.
    /// </summary>
    Task<bool> SetAsync(string key, string value, CancellationToken ct = default);
    Task SeedDefaultsAsync(CancellationToken ct = default);
}
