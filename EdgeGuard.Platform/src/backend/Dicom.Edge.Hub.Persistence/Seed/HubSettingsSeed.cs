using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Seed;

/// <summary>
/// Idempotent seeder for the <c>system_settings</c> table.
/// <list type="bullet">
///   <item><see cref="SeedMissingAsync"/> — inserts only keys absent from the DB (safe for upgrades).</item>
///   <item><see cref="SeedIfEmptyAsync"/> — inserts all defaults only if the table is completely empty.</item>
/// </list>
/// </summary>
public static class HubSettingsSeed
{
    /// <summary>
    /// Called on every startup. Inserts any keys that were added in newer versions
    /// without touching keys that already exist (safe for upgrades).
    /// </summary>
    public static async Task SeedMissingAsync(HubDbContext ctx, CancellationToken ct = default)
    {
        var existing = await ctx.SystemSettings
            .Select(s => s.Id)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var missing = BuildDefaults()
            .Where(s => !existingSet.Contains(s.Id))
            .ToList();

        if (missing.Count == 0) return;

        await ctx.SystemSettings.AddRangeAsync(missing, ct);
        await ctx.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Inserts all defaults only if the table is completely empty.
    /// Useful for first-run initialization.
    /// </summary>
    public static async Task SeedIfEmptyAsync(HubDbContext ctx, CancellationToken ct = default)
    {
        if (await ctx.SystemSettings.AnyAsync(ct)) return;

        await ctx.SystemSettings.AddRangeAsync(BuildDefaults(), ct);
        await ctx.SaveChangesAsync(ct);
    }

    // ── Seed definitions ──────────────────────────────────────────────────────

    private static List<SystemSetting> BuildDefaults() =>
    [
        // ── HL7 ─────────────────────────────────────────────────────────────
        Row(HubSettingKeys.Hl7.TcpPort,                   "2575",   Cat.Hl7,      "TCP Port",                     VT.Int),
        Row(HubSettingKeys.Hl7.TcpEnabled,                "true",   Cat.Hl7,      "TCP Enabled",                  VT.Bool),
        Row(HubSettingKeys.Hl7.MaxConcurrentConnections,  "50",     Cat.Hl7,      "Max Concurrent Connections",   VT.Int),
        Row(HubSettingKeys.Hl7.MaxQueuedMessages,         "10000",  Cat.Hl7,      "Max Queued Messages",          VT.Int),
        Row(HubSettingKeys.Hl7.ProcessingWorkers,         "4",      Cat.Hl7,      "Processing Workers",           VT.Int),
        Row(HubSettingKeys.Hl7.ConnectionTimeoutMs,       "30000",  Cat.Hl7,      "Connection Timeout (ms)",      VT.Int),
        Row(HubSettingKeys.Hl7.BufferSize,                "65536",  Cat.Hl7,      "Buffer Size (bytes)",          VT.Int),

        // ── Dispatch ────────────────────────────────────────────────────────
        Row(HubSettingKeys.Dispatch.Enabled,              "true",   Cat.Dispatch, "Dispatch Enabled",             VT.Bool),
        Row(HubSettingKeys.Dispatch.BatchSize,            "10",     Cat.Dispatch, "Batch Size",                   VT.Int),
        Row(HubSettingKeys.Dispatch.IntervalSeconds,      "5",      Cat.Dispatch, "Interval (sec)",               VT.Int),
        Row(HubSettingKeys.Dispatch.MaxRetries,           "5",      Cat.Dispatch, "Max Retries",                  VT.Int),
        Row(HubSettingKeys.Dispatch.RetryDelaySeconds,    "30",     Cat.Dispatch, "Retry Delay (sec)",            VT.Int),
        Row(HubSettingKeys.Dispatch.TimeoutSeconds,       "60",     Cat.Dispatch, "Timeout (sec)",                VT.Int),

        // ── Queue ───────────────────────────────────────────────────────────
        Row(HubSettingKeys.Queue.MaxPendingMessages,      "5000",   Cat.Queue,    "Max Pending Messages",         VT.Int),
        Row(HubSettingKeys.Queue.PriorityBoostUrgent,     "true",   Cat.Queue,    "Priority Boost Urgent",        VT.Bool),
        Row(HubSettingKeys.Queue.RetentionDays,           "30",     Cat.Queue,    "Retention (days)",             VT.Int),

        // ── General ─────────────────────────────────────────────────────────
        Row(HubSettingKeys.General.HubName,               "EdgeGuard Hub",  Cat.General,  "Hub Name",             VT.String),
        Row(HubSettingKeys.General.HubVersion,            "1.0.0",          Cat.General,  "Hub Version",          VT.String, readOnly: true),
        Row(HubSettingKeys.General.Environment,           "Production",     Cat.General,  "Environment",          VT.String),

        // ── WhatsApp ────────────────────────────────────────────────────────
        Row(HubSettingKeys.WhatsApp.Enabled,                    "false",   Cat.WhatsApp, "WhatsApp Enabled",              VT.Bool),
        Row(HubSettingKeys.WhatsApp.EnableAutomaticDelivery,    "false",   Cat.WhatsApp, "Enable Automatic Delivery",     VT.Bool),
        Row(HubSettingKeys.WhatsApp.Provider,                   "Twilio",  Cat.WhatsApp, "Messaging Provider",            VT.String),
        EncRow(HubSettingKeys.WhatsApp.ProviderConfig,          "{}",      Cat.WhatsApp, "Provider Configuration (JSON)", VT.String),
        Row(HubSettingKeys.WhatsApp.DefaultCountryPrefix,       "+521",    Cat.WhatsApp, "Default Country Prefix",        VT.String),
        Row(HubSettingKeys.WhatsApp.RetryMaxAttempts,           "3",       Cat.WhatsApp, "Retry Max Attempts",            VT.Int),
        Row(HubSettingKeys.WhatsApp.RetryDelaySeconds,          "60",      Cat.WhatsApp, "Retry Delay (sec)",             VT.Int),

        // ── Background Jobs ──────────────────────────────────────────────────
        Row(HubSettingKeys.BackgroundJobs.EnableNodeHealth,          "true",   Cat.Jobs,      "Enable Node Health Evaluator",         VT.Bool),
        Row(HubSettingKeys.BackgroundJobs.NodeHealthIntervalSec,     "60",     Cat.Jobs,      "Node Health Eval Interval (sec)",      VT.Int),
        Row(HubSettingKeys.BackgroundJobs.EnableStudyCleanup,        "true",   Cat.Jobs,      "Enable Study Cleanup Evaluator",       VT.Bool),
        Row(HubSettingKeys.BackgroundJobs.StudyCleanupIntervalSec,   "300",    Cat.Jobs,      "Study Cleanup Eval Interval (sec)",    VT.Int),
        Row(HubSettingKeys.BackgroundJobs.EnableDataRetention,       "true",   Cat.Jobs,      "Enable Data Retention",                VT.Bool),
        Row(HubSettingKeys.BackgroundJobs.DataRetentionIntervalSec,  "3600",   Cat.Jobs,      "Data Retention Interval (sec)",        VT.Int),

        // ── Data Retention ───────────────────────────────────────────────────
        Row(HubSettingKeys.DataRetention.AuditLogDays,               "90",     Cat.Retention, "Audit Log Retention (days)",           VT.Int),
        Row(HubSettingKeys.DataRetention.Hl7MessageDays,             "30",     Cat.Retention, "HL7 Message Retention (days)",         VT.Int),
        Row(HubSettingKeys.DataRetention.HealthCheckDays,            "30",     Cat.Retention, "Health Check Retention (days)",        VT.Int),
        Row(HubSettingKeys.DataRetention.WhatsAppNotificationDays,   "60",     Cat.Retention, "WhatsApp Notification Retention (d)",  VT.Int),
        Row(HubSettingKeys.DataRetention.PacsSendAuditDays,          "90",     Cat.Retention, "PACS Send Audit Retention (days)",     VT.Int),
        Row(HubSettingKeys.DataRetention.StudyStatusAuditDays,       "90",     Cat.Retention, "Study Status Audit Retention (days)",  VT.Int),
        Row(HubSettingKeys.DataRetention.BatchSize,                  "1000",   Cat.Retention, "Retention Purge Batch Size",           VT.Int),
    ];

    private static SystemSetting Row(
        string key,
        string value,
        string category,
        string displayName,
        string valueType,
        bool readOnly = false) =>
        SystemSetting.Create(key, value, category, displayName, valueType, isReadOnly: readOnly);

    private static SystemSetting EncRow(
        string key,
        string value,
        string category,
        string displayName,
        string valueType) =>
        SystemSetting.Create(key, value, category, displayName, valueType, isEncrypted: true);

    // ── Aliases for cleaner seed table ────────────────────────────────────────
    private static class Cat
    {
        public const string Hl7 = "HL7";
        public const string Dispatch = "Dispatch";
        public const string Queue = "Queue";
        public const string General = "General";
        public const string WhatsApp = "WhatsApp";
        public const string Jobs = "BackgroundJobs";
        public const string Retention = "DataRetention";
    }

    private static class VT
    {
        public const string String = "string";
        public const string Int = "int";
        public const string Bool = "bool";
    }
}
