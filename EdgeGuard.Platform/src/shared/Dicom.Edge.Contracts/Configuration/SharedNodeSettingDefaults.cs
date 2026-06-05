namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Single source of truth for all node setting defaults.
/// Consumed by Node seed, Hub seed, and Hub Application for initializing
/// <c>NodeConfigurationProfile</c> rows for new nodes.
/// </summary>
public static class SharedNodeSettingDefaults
{
    private static readonly Lazy<IReadOnlyList<NodeSettingDefaultEntry>> _defaults =
        new(BuildAll);

    /// <summary>All default entries (~66 settings).</summary>
    public static IReadOnlyList<NodeSettingDefaultEntry> All => _defaults.Value;

    private static IReadOnlyList<NodeSettingDefaultEntry> BuildAll()
    {
        var cat = typeof(SharedNodeSettingCategories);
        var vt  = typeof(SharedNodeSettingValueTypes);

        return
        [
            // ── General ─────────────────────────────────────────────────────
            E(K.General.NodeName,     "EdgeNode-1",   C.General,  "Node Name",              V.String),
            E(K.General.AeTitle,      "EDGE_NODE",    C.General,  "AE Title (derived)",     V.String),
            E(K.General.Description,  "",             C.General,  "Description",            V.String),
            E(K.General.Location,     "",             C.General,  "Location",               V.String),
            E(K.General.FacilityName, "",             C.General,  "Facility Name",          V.String),
            E(K.General.Timezone,     "UTC",          C.General,  "Timezone",               V.String),
            E(K.General.Version,      "1.0.0",        C.General,  "Software Version",       V.String, readOnly: true),
            E(K.General.ContactEmail, "",             C.General,  "Contact Email",          V.String),
            E(K.General.ContactPhone, "",             C.General,  "Contact Phone",          V.String),

            // ── Hub ─────────────────────────────────────────────────────────
            E(K.Hub.Enabled,               "false",  C.Hub, "Hub Integration Enabled",       V.Bool),
            E(K.Hub.Protocol,              "https",  C.Hub, "Hub Protocol",                  V.String),
            E(K.Hub.Hostname,              "",       C.Hub, "Hub Hostname",                  V.String),
            E(K.Hub.Port,                  "443",    C.Hub, "Hub Port",                      V.Int),
            E(K.Hub.BasePath,              "/api",   C.Hub, "Hub API Base Path",             V.String),
            E(K.Hub.ApiKey,                "",       C.Hub, "Hub API Key",                   V.String),
            E(K.Hub.TimeoutSeconds,        "30",     C.Hub, "Hub Request Timeout (sec)",     V.Int),
            E(K.Hub.HeartbeatIntervalSec,  "60",     C.Hub, "Heartbeat Interval (sec)",      V.Int),
            E(K.Hub.RegisterOnStartup,     "true",   C.Hub, "Register on Startup",           V.Bool),
            E(K.Hub.PullConfigOnStartup,   "true",   C.Hub, "Pull Config on Startup",        V.Bool),
            E(K.Hub.PullConfigIntervalMin, "15",     C.Hub, "Config Pull Interval (min)",    V.Int),
            E(K.Hub.TlsVerifyCertificate,  "true",   C.Hub, "Verify TLS Certificate",        V.Bool),
            E(K.Hub.MaxReconnectAttempts,  "5",      C.Hub, "Max Reconnect Attempts",        V.Int),
            E(K.Hub.ReconnectDelaySeconds, "30",     C.Hub, "Reconnect Delay (sec)",         V.Int),

            // ── DICOM ───────────────────────────────────────────────────────
            E(K.Dicom.Enabled,                   "true",      C.Dicom, "DICOM Server Enabled",            V.Bool),
            E(K.Dicom.ValidateCallingAe,         "false",     C.Dicom, "Validate Calling AE Title",       V.Bool),
            E(K.Dicom.AllowedAeTitles,           "[]",        C.Dicom, "Allowed AE Titles (JSON array)",  V.Json),
            E(K.Dicom.MaxAssociations,           "50",        C.Dicom, "Max Concurrent Associations",     V.Int),
            E(K.Dicom.Port,                      "11112",     C.Dicom, "DICOM Listen Port",               V.Int),
            E(K.Dicom.AeTitle,                   "EDGE_NODE", C.Dicom, "DICOM AE Title",                  V.String),
            E(K.Dicom.StudyCompletionTimeoutSec, "30",        C.Dicom, "Study Completion Timeout (sec)",   V.Int),
            E(K.Dicom.AssociationTimeoutSec,     "30",        C.Dicom, "Association Timeout (sec)",        V.Int),
            E(K.Dicom.DimseTimeoutSec,           "600",       C.Dicom, "DIMSE Timeout (sec)",              V.Int),
            E(K.Dicom.MaxPduLength,              "262144",    C.Dicom, "Max PDU Length (bytes)",            V.Int),
            E(K.Dicom.MwlEnabled,                "true",      C.Dicom, "MWL C-FIND SCP Enabled",           V.Bool),
            E(K.Dicom.CEchoEnabled,              "true",      C.Dicom, "C-ECHO (Verification) SCP Enabled", V.Bool),
            E(K.Dicom.QrEnabled,                 "true",      C.Dicom, "Query/Retrieve (Q/R) SCP Enabled",  V.Bool),

            // ── Cleanup ─────────────────────────────────────────────────────
            E(K.Cleanup.Enabled,            "true",  C.Cleanup, "Auto-Cleanup Enabled",          V.Bool),
            E(K.Cleanup.RetainDays,         "30",    C.Cleanup, "Retain All Studies (days)",     V.Int),
            E(K.Cleanup.RetainSentDays,     "7",     C.Cleanup, "Retain Sent Studies (days)",    V.Int),
            E(K.Cleanup.RetainFailedDays,   "90",    C.Cleanup, "Retain Failed Studies (days)",  V.Int),
            E(K.Cleanup.MaxStorageGb,       "100",   C.Cleanup, "Max Storage Threshold (GB)",    V.Int),
            E(K.Cleanup.RunIntervalMinutes, "60",    C.Cleanup, "Cleanup Run Interval (min)",    V.Int),
            E(K.Cleanup.DeleteArchived,     "true",  C.Cleanup, "Delete Archived Studies",       V.Bool),

            // ── Transfer ────────────────────────────────────────────────────
            E(K.Transfer.MaxRetries,        "5",   C.Transfer, "Max Retry Attempts",          V.Int),
            E(K.Transfer.RetryBaseDelaySec, "60",  C.Transfer, "Retry Base Delay (sec)",      V.Int),
            E(K.Transfer.TimeoutSeconds,    "300", C.Transfer, "Transfer Timeout (sec)",      V.Int),
            E(K.Transfer.MaxConcurrent,     "3",   C.Transfer, "Max Concurrent Transfers",    V.Int),

            // ── Security ────────────────────────────────────────────────────
            E(K.Security.RequireTls,         "false", C.Security, "Require TLS for DICOM",           V.Bool),
            E(K.Security.AuditRetentionDays, "365",   C.Security, "Audit Log Retention (days)",      V.Int),

            // ── Storage ─────────────────────────────────────────────────────
            E(K.Storage.RootPath,    "./data",    C.Storage, "DICOM Storage Root Path", V.String),
            E(K.Storage.ArchivePath, "./archive", C.Storage, "Archive Root Path",        V.String),

            // ── PACS Sender ─────────────────────────────────────────────────
            E(K.PacsSender.Enabled,                   "true",     C.PacsSender, "PACS Sender Enabled",       V.Bool),
            E(K.PacsSender.LocalAeTitle,              "EDGE_NODE", C.PacsSender, "Local AE Title (derived)",  V.String),
            E(K.PacsSender.MaxConcurrentSends,        "4",        C.PacsSender, "Max Concurrent Sends",      V.Int),
            E(K.PacsSender.TimeoutSeconds,            "120",      C.PacsSender, "Send Timeout (sec)",        V.Int),
            E(K.PacsSender.MaxRetries,                "3",        C.PacsSender, "Max Retries",               V.Int),
            E(K.PacsSender.RetryBaseDelaySeconds,     "10",       C.PacsSender, "Retry Base Delay (sec)",    V.Int),
            E(K.PacsSender.ProcessingIntervalSeconds, "5",        C.PacsSender, "Processing Interval (sec)", V.Int),

            // ── PACS C-ECHO ─────────────────────────────────────────────────
            E(K.PacsCEcho.Enabled,         "true", C.PacsCEcho, "C-ECHO Monitor Enabled",     V.Bool),
            E(K.PacsCEcho.IntervalSeconds, "120",  C.PacsCEcho, "C-ECHO Interval (sec)",      V.Int),
            E(K.PacsCEcho.Destinations,    "[]",   C.PacsCEcho, "C-ECHO Destinations (JSON)", V.Json),

            // ── Node API ────────────────────────────────────────────────────
            E(K.NodeApi.Port, "5120", C.NodeApi, "Node API Port", V.Int),

            // ── System (config sync metadata) ───────────────────────────────
            E(K.System.ConfigVersion,        "",      C.General, "Config Version Hash",         V.String, readOnly: true),
            E(K.System.LastConfigAppliedUtc,  "",      C.General, "Last Config Applied (UTC)",   V.String, readOnly: true),
            E(K.System.LastConfigSource,      "local", C.General, "Last Config Source",          V.String, readOnly: true),
        ];
    }

    // ── Shorthand aliases ────────────────────────────────────────────────────

    private static NodeSettingDefaultEntry E(
        string key, string value, string category,
        string displayName, string valueType, bool readOnly = false)
        => new(key, value, category, displayName, valueType, readOnly);

    // Key aliases
    private static class K
    {
        public static class General
        {
            public const string NodeName     = SharedNodeSettingKeys.General.NodeName;
            public const string AeTitle      = SharedNodeSettingKeys.General.AeTitle;
            public const string Description  = SharedNodeSettingKeys.General.Description;
            public const string Location     = SharedNodeSettingKeys.General.Location;
            public const string FacilityName = SharedNodeSettingKeys.General.FacilityName;
            public const string Timezone     = SharedNodeSettingKeys.General.Timezone;
            public const string Version      = SharedNodeSettingKeys.General.Version;
            public const string ContactEmail = SharedNodeSettingKeys.General.ContactEmail;
            public const string ContactPhone = SharedNodeSettingKeys.General.ContactPhone;
        }
        public static class Hub
        {
            public const string Enabled               = SharedNodeSettingKeys.Hub.Enabled;
            public const string Protocol              = SharedNodeSettingKeys.Hub.Protocol;
            public const string Hostname              = SharedNodeSettingKeys.Hub.Hostname;
            public const string Port                  = SharedNodeSettingKeys.Hub.Port;
            public const string BasePath              = SharedNodeSettingKeys.Hub.BasePath;
            public const string ApiKey                = SharedNodeSettingKeys.Hub.ApiKey;
            public const string TimeoutSeconds        = SharedNodeSettingKeys.Hub.TimeoutSeconds;
            public const string HeartbeatIntervalSec  = SharedNodeSettingKeys.Hub.HeartbeatIntervalSec;
            public const string RegisterOnStartup     = SharedNodeSettingKeys.Hub.RegisterOnStartup;
            public const string PullConfigOnStartup   = SharedNodeSettingKeys.Hub.PullConfigOnStartup;
            public const string PullConfigIntervalMin = SharedNodeSettingKeys.Hub.PullConfigIntervalMin;
            public const string TlsVerifyCertificate  = SharedNodeSettingKeys.Hub.TlsVerifyCertificate;
            public const string MaxReconnectAttempts  = SharedNodeSettingKeys.Hub.MaxReconnectAttempts;
            public const string ReconnectDelaySeconds = SharedNodeSettingKeys.Hub.ReconnectDelaySeconds;
        }
        public static class Dicom
        {
            public const string Enabled                   = SharedNodeSettingKeys.Dicom.Enabled;
            public const string ValidateCallingAe         = SharedNodeSettingKeys.Dicom.ValidateCallingAe;
            public const string AllowedAeTitles           = SharedNodeSettingKeys.Dicom.AllowedAeTitles;
            public const string MaxAssociations           = SharedNodeSettingKeys.Dicom.MaxAssociations;
            public const string Port                      = SharedNodeSettingKeys.Dicom.Port;
            public const string AeTitle                   = SharedNodeSettingKeys.Dicom.AeTitle;
            public const string StudyCompletionTimeoutSec = SharedNodeSettingKeys.Dicom.StudyCompletionTimeoutSec;
            public const string AssociationTimeoutSec     = SharedNodeSettingKeys.Dicom.AssociationTimeoutSec;
            public const string DimseTimeoutSec           = SharedNodeSettingKeys.Dicom.DimseTimeoutSec;
            public const string MaxPduLength              = SharedNodeSettingKeys.Dicom.MaxPduLength;
            public const string MwlEnabled                = SharedNodeSettingKeys.Dicom.MwlEnabled;
            public const string CEchoEnabled              = SharedNodeSettingKeys.Dicom.CEchoEnabled;
            public  const string QrEnabled                 = SharedNodeSettingKeys.Dicom.QrEnabled;
        }
        public static class Cleanup
        {
            public const string Enabled            = SharedNodeSettingKeys.Cleanup.Enabled;
            public const string RetainDays         = SharedNodeSettingKeys.Cleanup.RetainDays;
            public const string RetainSentDays     = SharedNodeSettingKeys.Cleanup.RetainSentDays;
            public const string RetainFailedDays   = SharedNodeSettingKeys.Cleanup.RetainFailedDays;
            public const string MaxStorageGb       = SharedNodeSettingKeys.Cleanup.MaxStorageGb;
            public const string RunIntervalMinutes = SharedNodeSettingKeys.Cleanup.RunIntervalMinutes;
            public const string DeleteArchived     = SharedNodeSettingKeys.Cleanup.DeleteArchived;
        }
        public static class Transfer
        {
            public const string MaxRetries        = SharedNodeSettingKeys.Transfer.MaxRetries;
            public const string RetryBaseDelaySec = SharedNodeSettingKeys.Transfer.RetryBaseDelaySec;
            public const string TimeoutSeconds    = SharedNodeSettingKeys.Transfer.TimeoutSeconds;
            public const string MaxConcurrent     = SharedNodeSettingKeys.Transfer.MaxConcurrent;
        }
        public static class Security
        {
            public const string RequireTls         = SharedNodeSettingKeys.Security.RequireTls;
            public const string AuditRetentionDays = SharedNodeSettingKeys.Security.AuditRetentionDays;
        }
        public static class Storage
        {
            public const string RootPath    = SharedNodeSettingKeys.Storage.RootPath;
            public const string ArchivePath = SharedNodeSettingKeys.Storage.ArchivePath;
        }
        public static class PacsSender
        {
            public const string Enabled                   = SharedNodeSettingKeys.PacsSender.Enabled;
            public const string LocalAeTitle              = SharedNodeSettingKeys.PacsSender.LocalAeTitle;
            public const string MaxConcurrentSends        = SharedNodeSettingKeys.PacsSender.MaxConcurrentSends;
            public const string TimeoutSeconds            = SharedNodeSettingKeys.PacsSender.TimeoutSeconds;
            public const string MaxRetries                = SharedNodeSettingKeys.PacsSender.MaxRetries;
            public const string RetryBaseDelaySeconds     = SharedNodeSettingKeys.PacsSender.RetryBaseDelaySeconds;
            public const string ProcessingIntervalSeconds = SharedNodeSettingKeys.PacsSender.ProcessingIntervalSeconds;
        }
        public static class PacsCEcho
        {
            public const string Enabled         = SharedNodeSettingKeys.PacsCEcho.Enabled;
            public const string IntervalSeconds = SharedNodeSettingKeys.PacsCEcho.IntervalSeconds;
            public const string Destinations    = SharedNodeSettingKeys.PacsCEcho.Destinations;
        }
        public static class NodeApi
        {
            public const string Port = SharedNodeSettingKeys.NodeApi.Port;
        }
        public static class System
        {
            public const string ConfigVersion        = SharedNodeSettingKeys.System.ConfigVersion;
            public const string LastConfigAppliedUtc  = SharedNodeSettingKeys.System.LastConfigAppliedUtc;
            public const string LastConfigSource      = SharedNodeSettingKeys.System.LastConfigSource;
        }
    }

    private static class C
    {
        public const string General    = SharedNodeSettingCategories.General;
        public const string Hub        = SharedNodeSettingCategories.Hub;
        public const string Dicom      = SharedNodeSettingCategories.Dicom;
        public const string Cleanup    = SharedNodeSettingCategories.Cleanup;
        public const string Transfer   = SharedNodeSettingCategories.Transfer;
        public const string Security   = SharedNodeSettingCategories.Security;
        public const string Storage    = SharedNodeSettingCategories.Storage;
        public const string PacsSender = SharedNodeSettingCategories.PacsSender;
        public const string PacsCEcho  = SharedNodeSettingCategories.PacsCEcho;
        public const string NodeApi    = SharedNodeSettingCategories.NodeApi;
    }

    private static class V
    {
        public const string String = SharedNodeSettingValueTypes.String;
        public const string Int    = SharedNodeSettingValueTypes.Int;
        public const string Bool   = SharedNodeSettingValueTypes.Bool;
        public const string Json   = SharedNodeSettingValueTypes.Json;
    }
}
