using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Dicom.Edge.Hub.Persistence.Configuration;

/// <summary>
/// Custom <see cref="IConfigurationProvider"/> that reads <c>system_settings</c> from
/// PostgreSQL via raw ADO.NET (Npgsql) and maps DB keys to <c>IConfiguration</c> paths.
/// This bridges the DB settings to the <c>IOptions&lt;T&gt;</c> pattern transparently.
/// </summary>
internal sealed class HubDatabaseConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;

    public HubDatabaseConfigurationProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT key, value FROM system_settings";

            using var reader = cmd.ExecuteReader();
            var dbSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                dbSettings[key] = value;
            }

            MapHl7Listener(dbSettings, data);
            MapMessageQueue(dbSettings, data);
            MapBackgroundJobs(dbSettings, data);
            MapDataRetention(dbSettings, data);
        }
        catch
        {
            // DB not ready yet (first run, migrations pending).
            // IOptions<T> will use defaults from the Options classes.
        }

        Data = data!;
    }

    // ── Hl7Listener ──────────────────────────────────────────────────────────

    private static void MapHl7Listener(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, HubSettingKeys.Hl7.TcpPort,                  "Hl7Listener:Port");
        Map(db, cfg, HubSettingKeys.Hl7.TcpEnabled,               "Hl7Listener:Enabled");
        Map(db, cfg, HubSettingKeys.Hl7.MaxConcurrentConnections, "Hl7Listener:MaxConcurrentConnections");
        Map(db, cfg, HubSettingKeys.Hl7.MaxQueuedMessages,        "Hl7Listener:MaxQueuedMessages");
        Map(db, cfg, HubSettingKeys.Hl7.ProcessingWorkers,        "Hl7Listener:ProcessingWorkers");
        Map(db, cfg, HubSettingKeys.Hl7.ConnectionTimeoutMs,      "Hl7Listener:ConnectionTimeoutMs");
        Map(db, cfg, HubSettingKeys.Hl7.BufferSize,               "Hl7Listener:BufferSize");
    }

    // ── MessageQueue ─────────────────────────────────────────────────────────

    private static void MapMessageQueue(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, HubSettingKeys.Dispatch.Enabled,           "MessageQueue:DispatchEnabled");
        Map(db, cfg, HubSettingKeys.Dispatch.BatchSize,         "MessageQueue:DispatchBatchSize");
        Map(db, cfg, HubSettingKeys.Dispatch.IntervalSeconds,   "MessageQueue:DispatchIntervalSeconds");
        Map(db, cfg, HubSettingKeys.Dispatch.MaxRetries,        "MessageQueue:MaxRetries");
        Map(db, cfg, HubSettingKeys.Dispatch.RetryDelaySeconds, "MessageQueue:RetryDelaySeconds");
        Map(db, cfg, HubSettingKeys.Dispatch.TimeoutSeconds,    "MessageQueue:DispatchTimeoutSeconds");
        Map(db, cfg, HubSettingKeys.Queue.MaxPendingMessages,   "MessageQueue:MaxPendingMessages");
        Map(db, cfg, HubSettingKeys.Queue.RetentionDays,        "MessageQueue:RetentionDays");
    }

    // ── HubBackgroundJobs ────────────────────────────────────────────────────

    private static void MapBackgroundJobs(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, HubSettingKeys.BackgroundJobs.EnableNodeHealth,         "HubBackgroundJobs:EnableNodeHealthEvaluator");
        Map(db, cfg, HubSettingKeys.BackgroundJobs.NodeHealthIntervalSec,    "HubBackgroundJobs:NodeHealthEvaluationIntervalSeconds");
        Map(db, cfg, HubSettingKeys.BackgroundJobs.EnableStudyCleanup,       "HubBackgroundJobs:EnableStudyCleanupEvaluator");
        Map(db, cfg, HubSettingKeys.BackgroundJobs.StudyCleanupIntervalSec,  "HubBackgroundJobs:StudyCleanupEvaluationIntervalSeconds");
        Map(db, cfg, HubSettingKeys.BackgroundJobs.EnableDataRetention,      "HubBackgroundJobs:EnableDataRetention");
        Map(db, cfg, HubSettingKeys.BackgroundJobs.DataRetentionIntervalSec, "HubBackgroundJobs:DataRetentionIntervalSeconds");
    }

    // ── DataRetention ────────────────────────────────────────────────────────

    private static void MapDataRetention(
        Dictionary<string, string> db,
        Dictionary<string, string?> cfg)
    {
        Map(db, cfg, HubSettingKeys.DataRetention.AuditLogDays,             "HubBackgroundJobs:DataRetention:AuditLogRetentionDays");
        Map(db, cfg, HubSettingKeys.DataRetention.Hl7MessageDays,           "HubBackgroundJobs:DataRetention:Hl7MessageRetentionDays");
        Map(db, cfg, HubSettingKeys.DataRetention.HealthCheckDays,          "HubBackgroundJobs:DataRetention:HealthCheckRetentionDays");
        Map(db, cfg, HubSettingKeys.DataRetention.NotificationDays,         "HubBackgroundJobs:DataRetention:NotificationOutboxRetentionDays");
        Map(db, cfg, HubSettingKeys.DataRetention.NodeOutboxDays,           "HubBackgroundJobs:DataRetention:NodeOutboxRetentionDays");
        Map(db, cfg, HubSettingKeys.DataRetention.PacsSendAuditDays,        "HubBackgroundJobs:DataRetention:PacsSendAuditRetentionDays");
        Map(db, cfg, HubSettingKeys.DataRetention.StudyStatusAuditDays,     "HubBackgroundJobs:DataRetention:StudyStatusAuditRetentionDays");
        Map(db, cfg, HubSettingKeys.DataRetention.BatchSize,                "HubBackgroundJobs:DataRetention:BatchSize");
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
