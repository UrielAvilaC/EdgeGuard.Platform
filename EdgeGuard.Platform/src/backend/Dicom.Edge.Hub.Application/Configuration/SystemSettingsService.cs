using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Configuration;

public sealed class SystemSettingsService : ISystemSettingsService
{
    private readonly ISystemSettingRepository _repository;
    private readonly ILogger<SystemSettingsService> _logger;

    public SystemSettingsService(ISystemSettingRepository repository, ILogger<SystemSettingsService> logger)
    {
        _repository = repository;
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
        _logger.LogInformation("System setting '{Key}' updated", key);
    }

    public async Task SeedDefaultsAsync(CancellationToken ct = default)
    {
        var defaults = new (string Key, string Value, string Category, string Display, string Type, string? Desc, bool ReadOnly)[]
        {
            (HubSettingKeys.Hl7.TcpPort, "8001", "HL7", "HL7 TCP Port", "int", "TCP port for HL7 listener", false),
            (HubSettingKeys.Hl7.TcpEnabled, "true", "HL7", "HL7 TCP Enabled", "bool", "Enable HL7 TCP listener", false),
            (HubSettingKeys.Hl7.MaxConcurrentConnections, "100", "HL7", "Max Concurrent Connections", "int", null, false),
            (HubSettingKeys.Hl7.MaxQueuedMessages, "1000", "HL7", "Max Queued Messages", "int", null, false),
            (HubSettingKeys.Hl7.ProcessingWorkers, "4", "HL7", "Processing Workers", "int", null, false),
            (HubSettingKeys.Hl7.ConnectionTimeoutMs, "300000", "HL7", "Connection Timeout (ms)", "int", null, false),
            (HubSettingKeys.Hl7.BufferSize, "8192", "HL7", "Buffer Size", "int", null, false),
            (HubSettingKeys.Dispatch.Enabled, "true", "Dispatch", "Dispatch Enabled", "bool", null, false),
            (HubSettingKeys.Dispatch.BatchSize, "20", "Dispatch", "Dispatch Batch Size", "int", null, false),
            (HubSettingKeys.Dispatch.IntervalSeconds, "5", "Dispatch", "Dispatch Interval (s)", "int", null, false),
            (HubSettingKeys.Dispatch.MaxRetries, "5", "Dispatch", "Max Retries", "int", null, false),
            (HubSettingKeys.Dispatch.RetryDelaySeconds, "30", "Dispatch", "Retry Delay (s)", "int", null, false),
            (HubSettingKeys.Dispatch.TimeoutSeconds, "30", "Dispatch", "Dispatch Timeout (s)", "int", null, false),
            (HubSettingKeys.Queue.MaxPendingMessages, "10000", "Queue", "Max Pending Messages", "int", null, false),
            (HubSettingKeys.Queue.RetentionDays, "30", "Queue", "Retention Days", "int", null, false),
            (HubSettingKeys.General.HubName, "EdgeGuard Hub", "General", "Hub Name", "string", null, false),
            (HubSettingKeys.General.HubVersion, "1.0.0", "General", "Hub Version", "string", null, true),
        };

        foreach (var d in defaults)
        {
            var existing = await _repository.GetByKeyAsync(d.Key, ct);
            if (existing is not null) continue;

            var setting = SystemSetting.Create(d.Key, d.Value, d.Category, d.Display, d.Type, d.Desc, d.ReadOnly);
            await _repository.AddAsync(setting, ct);
        }

        _logger.LogInformation("System settings defaults seeded");
    }

    private static SystemSettingDto Map(SystemSetting s) =>
        new(s.Id, s.Value, s.Category, s.DisplayName, s.ValueType, s.Description, s.IsReadOnly);
}
