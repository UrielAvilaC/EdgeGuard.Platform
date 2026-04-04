namespace Dicom.Edge.Abstractions.Persistence;

/// <summary>
/// Provides typed, in-memory-cached access to node settings persisted in the database.
/// All read operations are served from a warm in-memory cache.
/// Write operations persist to the database and update the cache atomically.
/// Grouped helper methods return strongly-typed config records per category.
/// </summary>
public interface INodeSettingsService
{
    // ── Core access ───────────────────────────────────────────────────────────

    /// <summary>Gets a typed setting value by key. Throws if key is missing.</summary>
    Task<T> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>Gets a typed setting value or returns the provided default.</summary>
    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default);

    /// <summary>Persists a setting value to the database and refreshes cache.</summary>
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);

    /// <summary>Returns all key/value pairs for a given category.</summary>
    Task<IReadOnlyDictionary<string, string>> GetCategoryAsync(
        string category, CancellationToken ct = default);

    /// <summary>
    /// Applies a batch of key/value pairs atomically.
    /// Used when the Hub pushes a configuration update.
    /// Read-only keys are silently skipped.
    /// </summary>
    Task ApplyBatchAsync(
        IReadOnlyDictionary<string, string> values, CancellationToken ct = default);

    /// <summary>Reloads the in-memory cache from the database.</summary>
    Task ReloadAsync(CancellationToken ct = default);

    // ── Grouped helpers ───────────────────────────────────────────────────────

    /// <summary>Returns all Hub connectivity and sync settings.</summary>
    Task<HubConfig> GetHubConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all DICOM server and association settings.</summary>
    Task<DicomConfig> GetDicomConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all study lifecycle auto-cleanup settings.</summary>
    Task<CleanupConfig> GetCleanupConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all Hub transfer and retry settings.</summary>
    Task<TransferConfig> GetTransferConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all storage path settings.</summary>
    Task<StorageConfig> GetStorageConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all security and audit settings.</summary>
    Task<SecurityConfig> GetSecurityConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all node identity and contact settings.</summary>
    Task<GeneralConfig> GetGeneralConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all PACS sender settings.</summary>
    Task<PacsSenderConfig> GetPacsSenderConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all PACS C-ECHO connectivity check settings.</summary>
    Task<PacsCEchoConfig> GetPacsCEchoConfigAsync(CancellationToken ct = default);

    /// <summary>Returns all Node API settings.</summary>
    Task<NodeApiConfig> GetNodeApiConfigAsync(CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────────────────────────
// Config records — sealed, immutable, one per category
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Hub connectivity and remote configuration settings.</summary>
public sealed record HubConfig(
    bool   Enabled,
    string Protocol,
    string Hostname,
    int    Port,
    string BasePath,
    string ApiKey,
    int    TimeoutSeconds,
    int    HeartbeatIntervalSec,
    bool   RegisterOnStartup,
    bool   PullConfigOnStartup,
    int    PullConfigIntervalMin,
    bool   TlsVerifyCertificate,
    int    MaxReconnectAttempts,
    int    ReconnectDelaySeconds)
{
    /// <summary>Builds the full base URL: {Protocol}://{Hostname}:{Port}{BasePath}</summary>
    public string BuildBaseUrl() => $"{Protocol}://{Hostname}:{Port}{BasePath}";

    /// <summary>True when Hub is enabled and a hostname is configured.</summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(Hostname);
}

/// <summary>DICOM server, association and study completion settings.</summary>
public sealed record DicomConfig(
    bool         ValidateCallingAe,
    List<string> AllowedAeTitles,
    int          MaxAssociations,
    int          Port,
    string       AeTitle,
    int          StudyCompletionTimeoutSec);

/// <summary>Study lifecycle auto-cleanup thresholds.</summary>
public sealed record CleanupConfig(
    bool Enabled,
    int  RetainDays,
    int  RetainSentDays,
    int  RetainFailedDays,
    int  MaxStorageGb,
    int  RunIntervalMinutes,
    bool DeleteArchived);

/// <summary>Transfer retry and concurrency settings.</summary>
public sealed record TransferConfig(
    int MaxRetries,
    int RetryBaseDelaySec,
    int TimeoutSeconds,
    int MaxConcurrent);

/// <summary>Physical storage paths.</summary>
public sealed record StorageConfig(
    string RootPath,
    string ArchivePath);

/// <summary>Security and audit retention settings.</summary>
public sealed record SecurityConfig(
    bool RequireTls,
    int  AuditRetentionDays);

/// <summary>Node identity, location, and contact settings.</summary>
public sealed record GeneralConfig(
    string NodeName,
    string AeTitle,
    string Description,
    string Location,
    string FacilityName,
    string Timezone,
    string Version,
    string ContactEmail,
    string ContactPhone);

/// <summary>PACS sender (C-STORE SCU) settings.</summary>
public sealed record PacsSenderConfig(
    bool   Enabled,
    string LocalAeTitle,
    int    MaxConcurrentSends,
    int    TimeoutSeconds,
    int    MaxRetries,
    int    RetryBaseDelaySeconds,
    int    ProcessingIntervalSeconds);

/// <summary>PACS C-ECHO connectivity monitoring settings.</summary>
public sealed record PacsCEchoConfig(
    bool   Enabled,
    int    IntervalSeconds,
    string DestinationsJson);

/// <summary>Node REST API settings.</summary>
public sealed record NodeApiConfig(
    int Port);
