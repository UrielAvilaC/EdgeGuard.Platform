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

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        var entity = await _repository.GetByKeyAsync(key, ct);
        if (entity is null)
        {
            _logger.LogWarning("System setting '{Key}' not found", key);
            return;
        }

        entity.UpdateValue(value);
        await _repository.UpdateAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("System setting '{Key}' updated", key);
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
