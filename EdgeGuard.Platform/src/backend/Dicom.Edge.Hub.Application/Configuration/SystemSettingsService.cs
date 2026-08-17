using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Configuration;

public sealed class SystemSettingsService : ISystemSettingsService
{
    private readonly ISystemSettingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SystemSettingsService> _logger;

    public SystemSettingsService(
        ISystemSettingRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<SystemSettingsService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SystemSettingDto?> GetAsync(string key, CancellationToken ct = default)
    {
        var entity = await _repository.GetByKeyAsync(key, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        var entities = await _repository.GetByCategoryAsync(category, ct);
        return entities.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetAllAsync(ct);
        return entities.Select(Map).ToList();
    }

    public async Task<bool> SetAsync(string key, string value, CancellationToken ct = default)
    {
        var entity = await _repository.GetByKeyAsync(key, ct);

        if (entity is null)
        {
            // The key is declared and has a default, but its row was never inserted — a DB
            // seeded before the key existed. Create it now from the default definition;
            // otherwise this was an update against nothing and the caller got a false success.
            var definition = HubSettingsDefaults.Build()
                .FirstOrDefault(s => string.Equals(s.Id, key, StringComparison.OrdinalIgnoreCase));

            if (definition is null)
            {
                _logger.LogWarning("System setting '{Key}' is not a known setting — not persisted", key);
                return false;
            }

            definition.UpdateValue(value);
            await _repository.AddAsync(definition, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("System setting '{Key}' created from defaults and set", key);
            return true;
        }

        entity.UpdateValue(value);
        await _repository.UpdateAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("System setting '{Key}' updated", key);
        return true;
    }

    public async Task SeedDefaultsAsync(CancellationToken ct = default)
    {
        // Single source of truth shared with the startup seeder (HubSettingsSeed).
        foreach (var setting in HubSettingsDefaults.Build())
        {
            var existing = await _repository.GetByKeyAsync(setting.Id, ct);
            if (existing is not null) continue;

            await _repository.AddAsync(setting, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("System settings defaults seeded");
    }

    private static SystemSettingDto Map(SystemSetting s) =>
        new(s.Id, s.Value, s.Category, s.DisplayName, s.ValueType, s.Description, s.IsReadOnly);
}
