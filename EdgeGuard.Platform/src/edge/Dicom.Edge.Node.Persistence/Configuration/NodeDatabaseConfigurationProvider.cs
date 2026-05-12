using Dicom.Edge.Node.Persistence.Constants;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// Custom <see cref="IConfigurationProvider"/> that reads <c>node_settings</c> from
/// SQLite via raw ADO.NET and maps DB keys to <c>IConfiguration</c> paths.
/// This bridges the DB settings to the <c>IOptions&lt;T&gt;</c> pattern transparently.
/// </summary>
internal sealed class NodeDatabaseConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;

    public NodeDatabaseConfigurationProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Re-reads SQLite and fires <see cref="IOptionsMonitor{T}"/> change tokens.
    /// Called by <see cref="INodeConfigurationReloader"/> after every DB write.
    /// </summary>
    internal void TriggerReload()
    {
        Load();
        OnReload();
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT key, value FROM {Constants.TableNames.NodeSettings}";

            using var reader = cmd.ExecuteReader();
            var dbSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                dbSettings[key] = value;
            }

            MapNodeApi(dbSettings, data);
            MapHubConnection(dbSettings, data);
            MapHubConnectionIdentity(dbSettings, data);
            MapDicomServer(dbSettings, data);
            MapPacsSender(dbSettings, data);
            MapPacsCEcho(dbSettings, data);
            MapPacsDestination(dbSettings, data);
        }
        catch
        {
            // DB not ready yet (first run, migrations pending).
            // IOptions<T> will use defaults from the Options classes.
        }

        Data = data!;
    }

    // ── NodeApi ──────────────────────────────────────────────────────────────

    private static void MapNodeApi(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.NodeApi.Port, ConfigPaths.NodeApiPort);
    }

    // ── HubConnection ────────────────────────────────────────────────────────

    private static void MapHubConnection(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        // Hub.Enabled is intentionally NOT mapped from the database.
        // It is an infrastructure decision controlled exclusively via appsettings
        // so the Hub cannot remotely disable its own connection channel.
        Map(db, cfg, NodeSettingKeys.Hub.ApiKey,                ConfigPaths.HubApiKey);
        Map(db, cfg, NodeSettingKeys.Hub.NodeId,                ConfigPaths.HubNodeId);
        Map(db, cfg, NodeSettingKeys.Hub.TimeoutSeconds,        ConfigPaths.HubTimeoutSeconds);
        Map(db, cfg, NodeSettingKeys.Hub.HeartbeatIntervalSec,  ConfigPaths.HubHeartbeatIntervalSeconds);
        Map(db, cfg, NodeSettingKeys.Hub.RegisterOnStartup,     ConfigPaths.HubRegisterOnStartup);
        Map(db, cfg, NodeSettingKeys.Hub.MaxReconnectAttempts,  ConfigPaths.HubMaxReconnectAttempts);
        Map(db, cfg, NodeSettingKeys.Hub.ReconnectDelaySeconds, ConfigPaths.HubReconnectDelaySeconds);

        // Compose HubBaseUrl from individual DB keys only when Hostname is configured.
        // If Hostname is empty the appsettings value is preserved (higher precedence wins).
        if (db.TryGetValue(NodeSettingKeys.Hub.Protocol, out var protocol) &&
            db.TryGetValue(NodeSettingKeys.Hub.Hostname, out var hostname) &&
            !string.IsNullOrWhiteSpace(hostname))
        {
            var port = db.TryGetValue(NodeSettingKeys.Hub.Port, out var portStr)
                ? portStr : ConfigDefaults.HubPort;

            cfg[ConfigPaths.HubBaseUrl] = $"{protocol}://{hostname}:{port}";
        }

        // Map config pull interval (stored as minutes in DB, seconds in Options)
        if (db.TryGetValue(NodeSettingKeys.Hub.PullConfigIntervalMin, out var pullMin) &&
            int.TryParse(pullMin, out var minutes))
        {
            cfg[ConfigPaths.HubConfigPullIntervalSeconds] = (minutes * 60).ToString();
        }
    }

    // ── HubConnection identity (General + Dicom → HubConnection:*) ────────

    private static void MapHubConnectionIdentity(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.General.NodeName,     ConfigPaths.HubNodeName);
        Map(db, cfg, NodeSettingKeys.General.AeTitle,      ConfigPaths.HubAeTitle);
        Map(db, cfg, NodeSettingKeys.General.IpAddress,    ConfigPaths.HubIpAddress);
        Map(db, cfg, NodeSettingKeys.Dicom.Port,           ConfigPaths.HubPort);
        Map(db, cfg, NodeSettingKeys.General.ApiEndpoint,  ConfigPaths.HubApiEndpoint);
        Map(db, cfg, NodeSettingKeys.General.Location,     ConfigPaths.HubLocation);
        Map(db, cfg, NodeSettingKeys.General.FacilityName, ConfigPaths.HubFacilityName);
        Map(db, cfg, NodeSettingKeys.General.Version,      ConfigPaths.HubVersion);
    }

    // ── DicomServer ──────────────────────────────────────────────────────────

    private static void MapDicomServer(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.Dicom.Enabled,              ConfigPaths.DicomEnabled);
        Map(db, cfg, NodeSettingKeys.Dicom.AeTitle,              ConfigPaths.DicomAeTitle);
        Map(db, cfg, NodeSettingKeys.Dicom.Port,                 ConfigPaths.DicomPort);
        Map(db, cfg, NodeSettingKeys.Dicom.MaxAssociations,      ConfigPaths.DicomMaxClients);
        Map(db, cfg, NodeSettingKeys.Dicom.AssociationTimeoutSec,ConfigPaths.DicomAssociationTimeout);
        Map(db, cfg, NodeSettingKeys.Dicom.DimseTimeoutSec,      ConfigPaths.DicomDimseTimeout);
        Map(db, cfg, NodeSettingKeys.Dicom.MaxPduLength,         ConfigPaths.DicomMaxPduLength);
        Map(db, cfg, NodeSettingKeys.Dicom.MwlEnabled,           ConfigPaths.DicomMwlEnabled);
        Map(db, cfg, NodeSettingKeys.Dicom.CEchoEnabled,         ConfigPaths.DicomCEchoEnabled);
        Map(db, cfg, NodeSettingKeys.Dicom.QrEnabled,            ConfigPaths.DicomQrEnabled);
        Map(db, cfg, NodeSettingKeys.Dicom.ValidateCallingAe,    ConfigPaths.DicomValidateCallingAe);
        Map(db, cfg, NodeSettingKeys.Dicom.ValidateCalledAe,     ConfigPaths.DicomValidateCalledAe);

        // AllowedCallingAeTitles — JSON array → indexed IConfiguration keys
        if (db.TryGetValue(NodeSettingKeys.Dicom.AllowedAeTitles, out var aeTitlesJson) &&
            !string.IsNullOrWhiteSpace(aeTitlesJson) && aeTitlesJson != ConfigDefaults.EmptyJsonArray)
        {
            try
            {
                var titles = System.Text.Json.JsonSerializer.Deserialize<string[]>(aeTitlesJson);
                if (titles is { Length: > 0 })
                {
                    for (var i = 0; i < titles.Length; i++)
                        cfg[$"{ConfigPaths.DicomAllowedCallingPrefix}:{i}"] = titles[i];
                }
            }
            catch { /* malformed JSON — skip */ }
        }

        // AeTitleAliases — JSON array → indexed IConfiguration keys
        if (db.TryGetValue(NodeSettingKeys.Dicom.AeTitleAliases, out var aliasesJson) &&
            !string.IsNullOrWhiteSpace(aliasesJson) && aliasesJson != ConfigDefaults.EmptyJsonArray)
        {
            try
            {
                var aliases = System.Text.Json.JsonSerializer.Deserialize<string[]>(aliasesJson);
                if (aliases is { Length: > 0 })
                {
                    for (var i = 0; i < aliases.Length; i++)
                        cfg[$"{ConfigPaths.DicomAeTitleAliasesPrefix}:{i}"] = aliases[i];
                }
            }
            catch { /* malformed JSON — skip */ }
        }
    }

    // ── PacsSender ───────────────────────────────────────────────────────────

    private static void MapPacsSender(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.PacsSender.Enabled,                   ConfigPaths.PacsSenderEnabled);
        Map(db, cfg, NodeSettingKeys.PacsSender.LocalAeTitle,              ConfigPaths.PacsSenderLocalAeTitle);
        Map(db, cfg, NodeSettingKeys.PacsSender.MaxConcurrentSends,        ConfigPaths.PacsSenderMaxConcurrentSends);
        Map(db, cfg, NodeSettingKeys.PacsSender.TimeoutSeconds,            ConfigPaths.PacsSenderTimeoutSeconds);
        Map(db, cfg, NodeSettingKeys.PacsSender.MaxRetries,                ConfigPaths.PacsSenderMaxRetries);
        Map(db, cfg, NodeSettingKeys.PacsSender.RetryBaseDelaySeconds,     ConfigPaths.PacsSenderRetryBaseDelaySeconds);
        Map(db, cfg, NodeSettingKeys.PacsSender.ProcessingIntervalSeconds, ConfigPaths.PacsSenderProcessingIntervalSeconds);
    }

    // ── PacsCEcho ────────────────────────────────────────────────────────────

    private static void MapPacsCEcho(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.PacsCEcho.Enabled,         ConfigPaths.PacsCEchoEnabled);
        Map(db, cfg, NodeSettingKeys.PacsCEcho.IntervalSeconds, ConfigPaths.PacsCEchoIntervalSeconds);

        // Destinations is stored as JSON array of objects — map to indexed IConfiguration keys
        if (db.TryGetValue(NodeSettingKeys.PacsCEcho.Destinations, out var destJson) &&
            !string.IsNullOrWhiteSpace(destJson) && destJson != ConfigDefaults.EmptyJsonArray)
        {
            try
            {
                var destinations = System.Text.Json.JsonSerializer.Deserialize<
                    System.Text.Json.JsonElement[]>(destJson);

                if (destinations is { Length: > 0 })
                {
                    for (var i = 0; i < destinations.Length; i++)
                    {
                        var dest = destinations[i];
                        var prefix = $"{ConfigPaths.PacsCEchoDestinations}:{i}";

                        if (dest.TryGetProperty("Id", out var id))
                            cfg[$"{prefix}:Id"] = id.GetString();
                        if (dest.TryGetProperty("AeTitle", out var aeTitle))
                            cfg[$"{prefix}:AeTitle"] = aeTitle.GetString();
                        if (dest.TryGetProperty("Host", out var host))
                            cfg[$"{prefix}:Host"] = host.GetString();
                        if (dest.TryGetProperty("Port", out var port))
                            cfg[$"{prefix}:Port"] = port.GetRawText();
                        if (dest.TryGetProperty("UseTls", out var useTls))
                            cfg[$"{prefix}:UseTls"] = useTls.GetRawText();
                    }
                }
            }
            catch { /* malformed JSON — skip */ }
        }
    }

    // ── PacsDestination ──────────────────────────────────────────────────────

    private static void MapPacsDestination(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.PacsDestination.Host,    ConfigPaths.PacsDestinationHost);
        Map(db, cfg, NodeSettingKeys.PacsDestination.Port,    ConfigPaths.PacsDestinationPort);
        Map(db, cfg, NodeSettingKeys.PacsDestination.AeTitle, ConfigPaths.PacsDestinationAeTitle);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void Map(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg,
        string dbKey,
        string configPath)
    {
        if (db.TryGetValue(dbKey, out var value) && !string.IsNullOrEmpty(value))
            cfg[configPath] = value;
    }
}
