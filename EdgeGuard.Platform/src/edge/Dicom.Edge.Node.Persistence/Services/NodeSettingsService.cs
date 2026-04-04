using Dicom.Edge.Node.Persistence.Diagnostics;

namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Singleton implementation of <see cref="INodeSettingsService"/>.
/// Reads are served from an in-memory <see cref="ConcurrentDictionary{TKey,TValue}"/> cache
/// warmed at startup via <see cref="ReloadAsync"/>. Writes flush to SQLite and update cache atomically.
/// Uses <see cref="IDbContextFactory{TContext}"/> to avoid captive-dependency issues (Singleton + Scoped DbContext).
/// </summary>
public sealed class NodeSettingsService(
    IDbContextFactory<EdgeNodeDbContext> factory,
    ILogger<NodeSettingsService> logger) : INodeSettingsService, IDisposable
{
    private ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private bool _disposed;

    // ── Core access ───────────────────────────────────────────────────────────

    public async Task<T> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out var raw))
        {
            logger.LogDebug("Setting {Key} cache HIT (raw={Value})", key, raw);
            return Parse<T>(raw);
        }

        logger.LogDebug("Setting {Key} cache MISS — falling back to DB", key);
        await using var ctx = await factory.CreateDbContextAsync(ct);
        var entity = await ctx.NodeSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        if (entity is not null)
        {
            logger.LogDebug("Setting {Key} found in DB (raw={Value})", key, entity.Value);
            return Parse<T>(entity.Value);
        }

        logger.LogDebug("Setting {Key} not found in DB — returning default", key);
        return default!;
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out var raw))
        {
            logger.LogDebug("Setting {Key} cache HIT (raw={Value})", key, raw);
            return Parse<T>(raw);
        }

        logger.LogDebug("Setting {Key} cache MISS — falling back to DB (default={Default})", key, defaultValue);
        await using var ctx = await factory.CreateDbContextAsync(ct);
        var entity = await ctx.NodeSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        return entity is not null ? Parse<T>(entity.Value) : defaultValue;
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        using var activity = PersistenceActivitySource.StartSettingsSet(key);
        await using var ctx = await factory.CreateDbContextAsync(ct);
        var entity = await ctx.NodeSettings.FindAsync([key], ct);

        if (entity is null)
        {
            logger.LogWarning("Setting {Key} not found in DB — update skipped", key);
            return;
        }

        if (entity.IsReadOnly)
        {
            logger.LogWarning("Setting {Key} is read-only — update rejected", key);
            return;
        }

        var previousValue = entity.Value;
        entity.Value = Serialize(value);
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(ct);

        _cache[key] = entity.Value;
        logger.LogDebug("Setting {Key} updated: {Previous} → {New}", key, previousValue, entity.Value);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetCategoryAsync(
        string category, CancellationToken ct = default)
    {
        var prefix = category.ToLowerInvariant() + ".";
        var fromCache = _cache
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        if (fromCache.Count > 0)
        {
            logger.LogDebug("Category {Category} cache HIT: {Count} entries", category, fromCache.Count);
            return fromCache;
        }

        // Cache miss — fall back to DB (occurs before first ReloadAsync)
        logger.LogDebug("Category {Category} cache MISS — querying DB", category);
        await using var ctx = await factory.CreateDbContextAsync(ct);
        var result = await ctx.NodeSettings.AsNoTracking()
            .Where(s => s.Category == category)
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase, ct);
        logger.LogDebug("Category {Category} loaded {Count} entries from DB", category, result.Count);
        return result;
    }

    public async Task ApplyBatchAsync(
        IReadOnlyDictionary<string, string> values, CancellationToken ct = default)
    {
        using var activity = PersistenceActivitySource.StartSettingsBatch(values.Count);
        await using var ctx = await factory.CreateDbContextAsync(ct);
        var keys = values.Keys.ToList();

        var entities = await ctx.NodeSettings
            .Where(s => keys.Contains(s.Key) && !s.IsReadOnly)
            .ToListAsync(ct);

        var updated = 0;
        foreach (var entity in entities)
        {
            if (!values.TryGetValue(entity.Key, out var newValue)) continue;
            entity.Value = newValue;
            entity.UpdatedAt = DateTime.UtcNow;
            _cache[entity.Key] = newValue;
            updated++;
        }

        if (updated > 0)
            await ctx.SaveChangesAsync(ct);

        logger.LogInformation(
            "Batch applied {Applied}/{Requested} settings from Hub push",
            updated, values.Count);
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        using var activity = PersistenceActivitySource.StartSettingsReload();
        await _reloadLock.WaitAsync(ct);
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(ct);
            var all = await ctx.NodeSettings.AsNoTracking().ToListAsync(ct);

            var fresh = new ConcurrentDictionary<string, string>(
                all.ToDictionary(s => s.Key, s => s.Value),
                StringComparer.OrdinalIgnoreCase);

            Interlocked.Exchange(ref _cache, fresh);
            logger.LogDebug("NodeSettings cache reloaded: {Count} entries", all.Count);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    // ── Grouped config helpers ────────────────────────────────────────────────

    public async Task<HubConfig> GetHubConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.Hub, ct);
        return new HubConfig(
            Enabled:               B(d, NodeSettingKeys.Hub.Enabled,               false),
            Protocol:              S(d, NodeSettingKeys.Hub.Protocol,              "https"),
            Hostname:              S(d, NodeSettingKeys.Hub.Hostname,              ""),
            Port:                  I(d, NodeSettingKeys.Hub.Port,                  443),
            BasePath:              S(d, NodeSettingKeys.Hub.BasePath,              "/api"),
            ApiKey:                S(d, NodeSettingKeys.Hub.ApiKey,                ""),
            TimeoutSeconds:        I(d, NodeSettingKeys.Hub.TimeoutSeconds,        30),
            HeartbeatIntervalSec:  I(d, NodeSettingKeys.Hub.HeartbeatIntervalSec,  60),
            RegisterOnStartup:     B(d, NodeSettingKeys.Hub.RegisterOnStartup,     true),
            PullConfigOnStartup:   B(d, NodeSettingKeys.Hub.PullConfigOnStartup,   true),
            PullConfigIntervalMin: I(d, NodeSettingKeys.Hub.PullConfigIntervalMin, 15),
            TlsVerifyCertificate:  B(d, NodeSettingKeys.Hub.TlsVerifyCertificate,  true),
            MaxReconnectAttempts:  I(d, NodeSettingKeys.Hub.MaxReconnectAttempts,  5),
            ReconnectDelaySeconds: I(d, NodeSettingKeys.Hub.ReconnectDelaySeconds, 30));
    }

    public async Task<DicomConfig> GetDicomConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.Dicom, ct);
        var json = S(d, NodeSettingKeys.Dicom.AllowedAeTitles, "[]");
        var allowed = JsonSerializer.Deserialize<List<string>>(json) ?? [];

        return new DicomConfig(
            ValidateCallingAe:         B(d, NodeSettingKeys.Dicom.ValidateCallingAe,         false),
            AllowedAeTitles:           allowed,
            MaxAssociations:           I(d, NodeSettingKeys.Dicom.MaxAssociations,           50),
            Port:                      I(d, NodeSettingKeys.Dicom.Port,                      11112),
            AeTitle:                   S(d, NodeSettingKeys.Dicom.AeTitle,                   "EDGE_NODE"),
            StudyCompletionTimeoutSec: I(d, NodeSettingKeys.Dicom.StudyCompletionTimeoutSec, 30));
    }

    public async Task<CleanupConfig> GetCleanupConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.Cleanup, ct);
        return new CleanupConfig(
            Enabled:            B(d, NodeSettingKeys.Cleanup.Enabled,            true),
            RetainDays:         I(d, NodeSettingKeys.Cleanup.RetainDays,         30),
            RetainSentDays:     I(d, NodeSettingKeys.Cleanup.RetainSentDays,     7),
            RetainFailedDays:   I(d, NodeSettingKeys.Cleanup.RetainFailedDays,   90),
            MaxStorageGb:       I(d, NodeSettingKeys.Cleanup.MaxStorageGb,       100),
            RunIntervalMinutes: I(d, NodeSettingKeys.Cleanup.RunIntervalMinutes, 60),
            DeleteArchived:     B(d, NodeSettingKeys.Cleanup.DeleteArchived,     true));
    }

    public async Task<TransferConfig> GetTransferConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.Transfer, ct);
        return new TransferConfig(
            MaxRetries:        I(d, NodeSettingKeys.Transfer.MaxRetries,        5),
            RetryBaseDelaySec: I(d, NodeSettingKeys.Transfer.RetryBaseDelaySec, 60),
            TimeoutSeconds:    I(d, NodeSettingKeys.Transfer.TimeoutSeconds,    300),
            MaxConcurrent:     I(d, NodeSettingKeys.Transfer.MaxConcurrent,     3));
    }

    public async Task<StorageConfig> GetStorageConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.Storage, ct);
        return new StorageConfig(
            RootPath:    S(d, NodeSettingKeys.Storage.RootPath,    "./data"),
            ArchivePath: S(d, NodeSettingKeys.Storage.ArchivePath, "./archive"));
    }

    public async Task<SecurityConfig> GetSecurityConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.Security, ct);
        return new SecurityConfig(
            RequireTls:         B(d, NodeSettingKeys.Security.RequireTls,         false),
            AuditRetentionDays: I(d, NodeSettingKeys.Security.AuditRetentionDays, 365));
    }

    public async Task<GeneralConfig> GetGeneralConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.General, ct);
        return new GeneralConfig(
            NodeName:     S(d, NodeSettingKeys.General.NodeName,     "EdgeNode-1"),
            AeTitle:      S(d, NodeSettingKeys.General.AeTitle,      "EDGE_NODE"),
            Description:  S(d, NodeSettingKeys.General.Description,  ""),
            Location:     S(d, NodeSettingKeys.General.Location,     ""),
            FacilityName: S(d, NodeSettingKeys.General.FacilityName, ""),
            Timezone:     S(d, NodeSettingKeys.General.Timezone,     "UTC"),
            Version:      S(d, NodeSettingKeys.General.Version,      "1.0.0"),
            ContactEmail: S(d, NodeSettingKeys.General.ContactEmail, ""),
            ContactPhone: S(d, NodeSettingKeys.General.ContactPhone, ""));
    }

    public async Task<PacsSenderConfig> GetPacsSenderConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.PacsSender, ct);
        return new PacsSenderConfig(
            Enabled:                   B(d, NodeSettingKeys.PacsSender.Enabled,                   true),
            LocalAeTitle:              S(d, NodeSettingKeys.PacsSender.LocalAeTitle,              "EDGE_NODE"),
            MaxConcurrentSends:        I(d, NodeSettingKeys.PacsSender.MaxConcurrentSends,        2),
            TimeoutSeconds:            I(d, NodeSettingKeys.PacsSender.TimeoutSeconds,            120),
            MaxRetries:                I(d, NodeSettingKeys.PacsSender.MaxRetries,                3),
            RetryBaseDelaySeconds:     I(d, NodeSettingKeys.PacsSender.RetryBaseDelaySeconds,     30),
            ProcessingIntervalSeconds: I(d, NodeSettingKeys.PacsSender.ProcessingIntervalSeconds, 10));
    }

    public async Task<PacsCEchoConfig> GetPacsCEchoConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.PacsCEcho, ct);
        return new PacsCEchoConfig(
            Enabled:          B(d, NodeSettingKeys.PacsCEcho.Enabled,         true),
            IntervalSeconds:  I(d, NodeSettingKeys.PacsCEcho.IntervalSeconds, 60),
            DestinationsJson: S(d, NodeSettingKeys.PacsCEcho.Destinations,    "[]"));
    }

    public async Task<NodeApiConfig> GetNodeApiConfigAsync(CancellationToken ct = default)
    {
        var d = await GetCategoryAsync(NodeSettingCategories.NodeApi, ct);
        return new NodeApiConfig(
            Port: I(d, NodeSettingKeys.NodeApi.Port, 5050));
    }

    // ── Type parsing ──────────────────────────────────────────────────────────

    private static T Parse<T>(string raw)
    {
        var type = typeof(T);
        if (type == typeof(string))   return (T)(object)raw;
        if (type == typeof(bool))     return (T)(object)bool.Parse(raw);
        if (type == typeof(int))      return (T)(object)int.Parse(raw, CultureInfo.InvariantCulture);
        if (type == typeof(long))     return (T)(object)long.Parse(raw, CultureInfo.InvariantCulture);
        if (type == typeof(double))   return (T)(object)double.Parse(raw, CultureInfo.InvariantCulture);
        if (type == typeof(DateTime)) return (T)(object)DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (type == typeof(TimeSpan)) return (T)(object)TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
        // Fallback: JSON deserialize for complex types (List<string>, etc.)
        return JsonSerializer.Deserialize<T>(raw) ?? default!;
    }

    private static string Serialize<T>(T value) => value switch
    {
        string s    => s,
        bool b      => b.ToString().ToLowerInvariant(),
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
        int or long or double or float
                    => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
        _           => JsonSerializer.Serialize(value),
    };

    // ── Dict shorthand helpers ────────────────────────────────────────────────

    private static string S(IReadOnlyDictionary<string, string> d, string k, string def)
        => d.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) ? v : def;

    private static int I(IReadOnlyDictionary<string, string> d, string k, int def)
        => d.TryGetValue(k, out var v)
           && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : def;

    private static bool B(IReadOnlyDictionary<string, string> d, string k, bool def)
        => d.TryGetValue(k, out var v) && bool.TryParse(v, out var r) ? r : def;

    // ── Disposal ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _reloadLock.Dispose();
        _disposed = true;
    }
}
