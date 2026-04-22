namespace Dicom.Edge.Node.Persistence.Seed;

/// <summary>
/// Idempotent seeder for the <c>node_settings</c> table.
/// <list type="bullet">
///   <item><see cref="SeedMissingAsync"/> — inserts only keys absent from the DB (used on every startup).</item>
///   <item><see cref="SeedIfEmptyAsync"/> — inserts all defaults only if the table is completely empty.</item>
/// </list>
/// Read-only keys are never overwritten by either method.
/// </summary>
internal static class NodeSettingsSeed
{
    /// <summary>
    /// Called on every startup. Inserts any keys that were added in newer versions
    /// without touching keys that already exist (safe for upgrades).
    /// </summary>
    public static async Task SeedMissingAsync(
        EdgeNodeDbContext ctx, CancellationToken ct = default)
    {
        var existing = await ctx.NodeSettings
            .Select(s => s.Key)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var missing = BuildDefaults()
            .Where(s => !existingSet.Contains(s.Key))
            .ToList();

        if (missing.Count == 0) return;

        await ctx.NodeSettings.AddRangeAsync(missing, ct);
        await ctx.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Inserts all defaults only if the table is completely empty.
    /// Useful for first-run initialization.
    /// </summary>
    public static async Task SeedIfEmptyAsync(
        EdgeNodeDbContext ctx, CancellationToken ct = default)
    {
        if (await ctx.NodeSettings.AnyAsync(ct)) return;

        await ctx.NodeSettings.AddRangeAsync(BuildDefaults(), ct);
        await ctx.SaveChangesAsync(ct);
    }

    // ── Seed definitions ──────────────────────────────────────────────────────

    private static List<NodeSettingEntity> BuildDefaults() =>
    [
        // ── General ─────────────────────────────────────────────────────────
        Row(NodeSettingKeys.General.NodeName,     "EdgeNode-1",   Cat.General,  "Node Name",              VT.String),
        Row(NodeSettingKeys.General.AeTitle,      "EDGE_NODE",    Cat.General,  "AE Title",               VT.String),
        Row(NodeSettingKeys.General.Description,  "",             Cat.General,  "Description",            VT.String),
        Row(NodeSettingKeys.General.Location,     "",             Cat.General,  "Location",               VT.String),
        Row(NodeSettingKeys.General.FacilityName, "",             Cat.General,  "Facility Name",          VT.String),
        Row(NodeSettingKeys.General.Timezone,     "UTC",          Cat.General,  "Timezone",               VT.String),
        Row(NodeSettingKeys.General.Version,      "1.0.0",        Cat.General,  "Software Version",       VT.String, readOnly: true),
        Row(NodeSettingKeys.General.ContactEmail, "",             Cat.General,  "Contact Email",          VT.String),
        Row(NodeSettingKeys.General.ContactPhone, "",             Cat.General,  "Contact Phone",          VT.String),
        Row(NodeSettingKeys.General.IpAddress,    "127.0.0.1",    Cat.General,  "IP Address",             VT.String),
        Row(NodeSettingKeys.General.ApiEndpoint,  "",             Cat.General,  "API Endpoint",           VT.String),

        // ── Hub ─────────────────────────────────────────────────────────────
        Row(NodeSettingKeys.Hub.Enabled,               "false",  Cat.Hub, "Hub Integration Enabled",       VT.Bool),
        Row(NodeSettingKeys.Hub.Protocol,              "https",   Cat.Hub, "Hub Protocol",                  VT.String),
        Row(NodeSettingKeys.Hub.Hostname,              "",        Cat.Hub, "Hub Hostname",                  VT.String),
        Row(NodeSettingKeys.Hub.Port,                  "443",     Cat.Hub, "Hub Port",                      VT.Int),
        Row(NodeSettingKeys.Hub.BasePath,              "/api",    Cat.Hub, "Hub API Base Path",             VT.String),
        Row(NodeSettingKeys.Hub.ApiKey,                "",        Cat.Hub, "Hub API Key",                   VT.String),
        Row(NodeSettingKeys.Hub.TimeoutSeconds,        "30",      Cat.Hub, "Hub Request Timeout (sec)",     VT.Int),
        Row(NodeSettingKeys.Hub.HeartbeatIntervalSec,  "60",      Cat.Hub, "Heartbeat Interval (sec)",      VT.Int),
        Row(NodeSettingKeys.Hub.RegisterOnStartup,     "true",    Cat.Hub, "Register on Startup",           VT.Bool),
        Row(NodeSettingKeys.Hub.PullConfigOnStartup,   "true",    Cat.Hub, "Pull Config on Startup",        VT.Bool),
        Row(NodeSettingKeys.Hub.PullConfigIntervalMin, "15",      Cat.Hub, "Config Pull Interval (min)",    VT.Int),
        Row(NodeSettingKeys.Hub.TlsVerifyCertificate,  "true",    Cat.Hub, "Verify TLS Certificate",        VT.Bool),
        Row(NodeSettingKeys.Hub.MaxReconnectAttempts,  "5",       Cat.Hub, "Max Reconnect Attempts",        VT.Int),
        Row(NodeSettingKeys.Hub.ReconnectDelaySeconds, "30",      Cat.Hub, "Reconnect Delay (sec)",         VT.Int),

        // ── DICOM ────────────────────────────────────────────────────────────
        Row(NodeSettingKeys.Dicom.Enabled,                   "true",      Cat.Dicom, "DICOM Server Enabled",            VT.Bool),
        Row(NodeSettingKeys.Dicom.ValidateCallingAe,         "false",     Cat.Dicom, "Validate Calling AE Title",       VT.Bool),
        Row(NodeSettingKeys.Dicom.AllowedAeTitles,           "[]",        Cat.Dicom, "Allowed AE Titles (JSON array)",  VT.Json),
        Row(NodeSettingKeys.Dicom.MaxAssociations,           "50",        Cat.Dicom, "Max Concurrent Associations",     VT.Int),
        Row(NodeSettingKeys.Dicom.Port,                      "11112",     Cat.Dicom, "DICOM Listen Port",               VT.Int),
        Row(NodeSettingKeys.Dicom.AeTitle,                   "EDGE_NODE", Cat.Dicom, "DICOM AE Title",                  VT.String),
        Row(NodeSettingKeys.Dicom.StudyCompletionTimeoutSec, "30",        Cat.Dicom, "Study Completion Timeout (sec)",   VT.Int),
        Row(NodeSettingKeys.Dicom.AssociationTimeoutSec,     "30",        Cat.Dicom, "Association Timeout (sec)",        VT.Int),
        Row(NodeSettingKeys.Dicom.DimseTimeoutSec,           "600",       Cat.Dicom, "DIMSE Timeout (sec)",              VT.Int),
        Row(NodeSettingKeys.Dicom.MaxPduLength,              "262144",    Cat.Dicom, "Max PDU Length (bytes)",            VT.Int),
        Row(NodeSettingKeys.Dicom.MwlEnabled,                "true",      Cat.Dicom, "MWL C-FIND SCP Enabled",           VT.Bool),

        // ── Cleanup ──────────────────────────────────────────────────────────
        Row(NodeSettingKeys.Cleanup.Enabled,            "true",  Cat.Cleanup, "Auto-Cleanup Enabled",          VT.Bool),
        Row(NodeSettingKeys.Cleanup.RetainDays,         "30",    Cat.Cleanup, "Retain All Studies (days)",     VT.Int),
        Row(NodeSettingKeys.Cleanup.RetainSentDays,     "7",     Cat.Cleanup, "Retain Sent Studies (days)",    VT.Int),
        Row(NodeSettingKeys.Cleanup.RetainFailedDays,   "90",    Cat.Cleanup, "Retain Failed Studies (days)",  VT.Int),
        Row(NodeSettingKeys.Cleanup.MaxStorageGb,       "100",   Cat.Cleanup, "Max Storage Threshold (GB)",    VT.Int),
        Row(NodeSettingKeys.Cleanup.RunIntervalMinutes, "60",    Cat.Cleanup, "Cleanup Run Interval (min)",    VT.Int),
        Row(NodeSettingKeys.Cleanup.DeleteArchived,     "true",  Cat.Cleanup, "Delete Archived Studies",       VT.Bool),

        // ── Transfer ─────────────────────────────────────────────────────────
        Row(NodeSettingKeys.Transfer.MaxRetries,        "5",   Cat.Transfer, "Max Retry Attempts",          VT.Int),
        Row(NodeSettingKeys.Transfer.RetryBaseDelaySec, "60",  Cat.Transfer, "Retry Base Delay (sec)",      VT.Int),
        Row(NodeSettingKeys.Transfer.TimeoutSeconds,    "300", Cat.Transfer, "Transfer Timeout (sec)",      VT.Int),
        Row(NodeSettingKeys.Transfer.MaxConcurrent,     "3",   Cat.Transfer, "Max Concurrent Transfers",    VT.Int),

        // ── Security ─────────────────────────────────────────────────────────
        Row(NodeSettingKeys.Security.RequireTls,         "false", Cat.Security, "Require TLS for DICOM",           VT.Bool),
        Row(NodeSettingKeys.Security.AuditRetentionDays, "365",   Cat.Security, "Audit Log Retention (days)",      VT.Int),

        // ── Storage ──────────────────────────────────────────────────────────
        Row(NodeSettingKeys.Storage.RootPath,    "./data",      Cat.Storage, "DICOM Storage Root Path", VT.String),
        Row(NodeSettingKeys.Storage.ArchivePath, "./workspace", Cat.Storage, "Archive Root Path",        VT.String),

        // ── PACS Sender ──────────────────────────────────────────────────────
        Row(NodeSettingKeys.PacsSender.Enabled,                   "true",      Cat.PacsSender, "PACS Sender Enabled",               VT.Bool),
        Row(NodeSettingKeys.PacsSender.LocalAeTitle,              "EDGENODE",  Cat.PacsSender, "Local AE Title",                    VT.String),
        Row(NodeSettingKeys.PacsSender.MaxConcurrentSends,        "4",         Cat.PacsSender, "Max Concurrent Sends",              VT.Int),
        Row(NodeSettingKeys.PacsSender.TimeoutSeconds,            "120",       Cat.PacsSender, "Send Timeout (sec)",                VT.Int),
        Row(NodeSettingKeys.PacsSender.MaxRetries,                "3",         Cat.PacsSender, "Max Retries",                       VT.Int),
        Row(NodeSettingKeys.PacsSender.RetryBaseDelaySeconds,     "10",        Cat.PacsSender, "Retry Base Delay (sec)",            VT.Int),
        Row(NodeSettingKeys.PacsSender.ProcessingIntervalSeconds, "5",         Cat.PacsSender, "Processing Interval (sec)",         VT.Int),

        // ── PACS C-ECHO ──────────────────────────────────────────────────────
        Row(NodeSettingKeys.PacsCEcho.Enabled,         "true",  Cat.PacsCEcho, "C-ECHO Monitor Enabled",       VT.Bool),
        Row(NodeSettingKeys.PacsCEcho.IntervalSeconds,  "120",  Cat.PacsCEcho, "C-ECHO Interval (sec)",        VT.Int),
        Row(NodeSettingKeys.PacsCEcho.Destinations,     "[]",   Cat.PacsCEcho, "C-ECHO Destinations (JSON)",   VT.Json),

        // ── PACS Destination ─────────────────────────────────────────────────
        Row(NodeSettingKeys.PacsDestination.Host,    "",    Cat.PacsDestination, "PACS Destination Host",     VT.String),
        Row(NodeSettingKeys.PacsDestination.Port,    "104", Cat.PacsDestination, "PACS Destination Port",     VT.Int),
        Row(NodeSettingKeys.PacsDestination.AeTitle, "",    Cat.PacsDestination, "PACS Destination AE Title", VT.String),

        // ── Node API ─────────────────────────────────────────────────────────
        Row(NodeSettingKeys.NodeApi.Port, "5120", Cat.NodeApi, "Node API Port", VT.Int),

        // ── System (config sync metadata) ────────────────────────────────────
        Row(NodeSettingKeys.System.ConfigVersion,        "",      Cat.General, "Config Version Hash",       VT.String, readOnly: true),
        Row(NodeSettingKeys.System.LastConfigAppliedUtc,  "",      Cat.General, "Last Config Applied (UTC)", VT.String, readOnly: true),
        Row(NodeSettingKeys.System.LastConfigSource,      "local", Cat.General, "Last Config Source",        VT.String, readOnly: true),
    ];

    private static NodeSettingEntity Row(
        string key,
        string value,
        string category,
        string displayName,
        string valueType,
        bool   readOnly = false) => new()
    {
        Key         = key,
        Value       = value,
        Category    = category,
        DisplayName = displayName,
        ValueType   = valueType,
        IsReadOnly  = readOnly,
        CreatedAt   = DateTime.UtcNow,
        UpdatedAt   = DateTime.UtcNow,
    };

    // ── Aliases for cleaner seed table ────────────────────────────────────────
    private static class Cat
    {
        public const string General    = NodeSettingCategories.General;
        public const string Hub        = NodeSettingCategories.Hub;
        public const string Dicom      = NodeSettingCategories.Dicom;
        public const string Cleanup    = NodeSettingCategories.Cleanup;
        public const string Transfer   = NodeSettingCategories.Transfer;
        public const string Security   = NodeSettingCategories.Security;
        public const string Storage    = NodeSettingCategories.Storage;
        public const string PacsSender = NodeSettingCategories.PacsSender;
        public const string PacsCEcho       = NodeSettingCategories.PacsCEcho;
        public const string PacsDestination = NodeSettingCategories.PacsDestination;
        public const string NodeApi         = NodeSettingCategories.NodeApi;
    }

    private static class VT
    {
        public const string String = NodeSettingValueTypes.String;
        public const string Int    = NodeSettingValueTypes.Int;
        public const string Bool   = NodeSettingValueTypes.Bool;
        public const string Json   = NodeSettingValueTypes.Json;
    }
}
