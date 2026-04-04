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

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT key, value FROM node_settings";

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
            MapDicomServer(dbSettings, data);
            MapPacsSender(dbSettings, data);
            MapPacsCEcho(dbSettings, data);
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
        Map(db, cfg, NodeSettingKeys.NodeApi.Port, "NodeApi:Port");
    }

    // ── HubConnection ────────────────────────────────────────────────────────

    private static void MapHubConnection(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.Hub.Enabled,               "HubConnection:Enabled");
        Map(db, cfg, NodeSettingKeys.Hub.ApiKey,                "HubConnection:ApiKey");
        Map(db, cfg, NodeSettingKeys.Hub.TimeoutSeconds,        "HubConnection:TimeoutSeconds");
        Map(db, cfg, NodeSettingKeys.Hub.HeartbeatIntervalSec,  "HubConnection:HeartbeatIntervalSeconds");
        Map(db, cfg, NodeSettingKeys.Hub.RegisterOnStartup,     "HubConnection:RegisterOnStartup");
        Map(db, cfg, NodeSettingKeys.Hub.MaxReconnectAttempts,   "HubConnection:MaxReconnectAttempts");
        Map(db, cfg, NodeSettingKeys.Hub.ReconnectDelaySeconds,  "HubConnection:ReconnectDelaySeconds");

        // Compose HubBaseUrl from individual DB keys
        if (db.TryGetValue(NodeSettingKeys.Hub.Protocol, out var protocol) &&
            db.TryGetValue(NodeSettingKeys.Hub.Hostname, out var hostname) &&
            !string.IsNullOrWhiteSpace(hostname))
        {
            var port = db.TryGetValue(NodeSettingKeys.Hub.Port, out var portStr)
                ? portStr : "443";

            cfg["HubConnection:HubBaseUrl"] = $"{protocol}://{hostname}:{port}";
        }

        // Map config pull interval (stored as minutes in DB, seconds in Options)
        if (db.TryGetValue(NodeSettingKeys.Hub.PullConfigIntervalMin, out var pullMin) &&
            int.TryParse(pullMin, out var minutes))
        {
            cfg["HubConnection:ConfigPullIntervalSeconds"] = (minutes * 60).ToString();
        }
    }

    // ── DicomServer ──────────────────────────────────────────────────────────

    private static void MapDicomServer(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.Dicom.Enabled,              "DicomServer:Enabled");
        Map(db, cfg, NodeSettingKeys.Dicom.AeTitle,              "DicomServer:AeTitle");
        Map(db, cfg, NodeSettingKeys.Dicom.Port,                 "DicomServer:Port");
        Map(db, cfg, NodeSettingKeys.Dicom.MaxAssociations,      "DicomServer:MaxClients");
        Map(db, cfg, NodeSettingKeys.Dicom.AssociationTimeoutSec,"DicomServer:AssociationTimeoutSeconds");
        Map(db, cfg, NodeSettingKeys.Dicom.DimseTimeoutSec,      "DicomServer:DimseTimeoutSeconds");
        Map(db, cfg, NodeSettingKeys.Dicom.MaxPduLength,         "DicomServer:MaxPduLength");
        Map(db, cfg, NodeSettingKeys.Dicom.MwlEnabled,           "DicomServer:MwlEnabled");

        // AllowedCallingAeTitles is stored as JSON array — map to indexed IConfiguration keys
        if (db.TryGetValue(NodeSettingKeys.Dicom.AllowedAeTitles, out var aeTitlesJson) &&
            !string.IsNullOrWhiteSpace(aeTitlesJson) && aeTitlesJson != "[]")
        {
            try
            {
                var titles = System.Text.Json.JsonSerializer.Deserialize<string[]>(aeTitlesJson);
                if (titles is { Length: > 0 })
                {
                    for (var i = 0; i < titles.Length; i++)
                        cfg[$"DicomServer:AllowedCallingAeTitles:{i}"] = titles[i];
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
        Map(db, cfg, NodeSettingKeys.PacsSender.Enabled,                   "PacsSender:Enabled");
        Map(db, cfg, NodeSettingKeys.PacsSender.LocalAeTitle,              "PacsSender:LocalAeTitle");
        Map(db, cfg, NodeSettingKeys.PacsSender.MaxConcurrentSends,        "PacsSender:MaxConcurrentSends");
        Map(db, cfg, NodeSettingKeys.PacsSender.TimeoutSeconds,            "PacsSender:TimeoutSeconds");
        Map(db, cfg, NodeSettingKeys.PacsSender.MaxRetries,                "PacsSender:MaxRetries");
        Map(db, cfg, NodeSettingKeys.PacsSender.RetryBaseDelaySeconds,     "PacsSender:RetryBaseDelaySeconds");
        Map(db, cfg, NodeSettingKeys.PacsSender.ProcessingIntervalSeconds, "PacsSender:ProcessingIntervalSeconds");
    }

    // ── PacsCEcho ────────────────────────────────────────────────────────────

    private static void MapPacsCEcho(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, NodeSettingKeys.PacsCEcho.Enabled,         "PacsCEcho:Enabled");
        Map(db, cfg, NodeSettingKeys.PacsCEcho.IntervalSeconds, "PacsCEcho:IntervalSeconds");

        // Destinations is stored as JSON array of objects — map to indexed IConfiguration keys
        if (db.TryGetValue(NodeSettingKeys.PacsCEcho.Destinations, out var destJson) &&
            !string.IsNullOrWhiteSpace(destJson) && destJson != "[]")
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
                        var prefix = $"PacsCEcho:Destinations:{i}";

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
