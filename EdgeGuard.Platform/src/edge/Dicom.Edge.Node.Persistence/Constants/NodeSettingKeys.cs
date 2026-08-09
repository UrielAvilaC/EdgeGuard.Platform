using Dicom.Edge.Contracts.Configuration;

namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// All keys for the node_settings table, grouped by category using nested static classes.
/// Backward-compatible aliases to <see cref="SharedNodeSettingKeys"/>.
/// </summary>
public static class NodeSettingKeys
{
    // ── General ──────────────────────────────────────────────────────────────────
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
        public const string IpAddress    = SharedNodeSettingKeys.General.IpAddress;
        public const string ApiEndpoint  = SharedNodeSettingKeys.General.ApiEndpoint;
    }

    // ── Hub ───────────────────────────────────────────────────────────────────────
    public static class Hub
    {
        public const string Enabled               = SharedNodeSettingKeys.Hub.Enabled;
        public const string Protocol              = SharedNodeSettingKeys.Hub.Protocol;
        public const string Hostname              = SharedNodeSettingKeys.Hub.Hostname;
        public const string Port                  = SharedNodeSettingKeys.Hub.Port;
        public const string BasePath              = SharedNodeSettingKeys.Hub.BasePath;
        public const string ApiKey                = SharedNodeSettingKeys.Hub.ApiKey;
        public const string NodeId                = SharedNodeSettingKeys.Hub.NodeId;
        public const string TimeoutSeconds        = SharedNodeSettingKeys.Hub.TimeoutSeconds;
        public const string HeartbeatIntervalSec  = SharedNodeSettingKeys.Hub.HeartbeatIntervalSec;
        public const string RegisterOnStartup     = SharedNodeSettingKeys.Hub.RegisterOnStartup;
        public const string PullConfigOnStartup   = SharedNodeSettingKeys.Hub.PullConfigOnStartup;
        public const string PullConfigIntervalMin = SharedNodeSettingKeys.Hub.PullConfigIntervalMin;
        public const string TlsVerifyCertificate  = SharedNodeSettingKeys.Hub.TlsVerifyCertificate;
        public const string MaxReconnectAttempts  = SharedNodeSettingKeys.Hub.MaxReconnectAttempts;
        public const string ReconnectDelaySeconds = SharedNodeSettingKeys.Hub.ReconnectDelaySeconds;
    }

    // ── DICOM ─────────────────────────────────────────────────────────────────────
    public static class Dicom
    {
        public const string Enabled                   = SharedNodeSettingKeys.Dicom.Enabled;
        public const string ValidateCallingAe         = SharedNodeSettingKeys.Dicom.ValidateCallingAe;
        public const string ValidateCalledAe          = SharedNodeSettingKeys.Dicom.ValidateCalledAe;
        public const string AllowedAeTitles           = SharedNodeSettingKeys.Dicom.AllowedAeTitles;
        public const string AeTitleAliases            = SharedNodeSettingKeys.Dicom.AeTitleAliases;
        public const string MaxAssociations           = SharedNodeSettingKeys.Dicom.MaxAssociations;
        public const string Port                      = SharedNodeSettingKeys.Dicom.Port;
        public const string AeTitle                   = SharedNodeSettingKeys.Dicom.AeTitle;
        public const string StudyCompletionTimeoutSec = SharedNodeSettingKeys.Dicom.StudyCompletionTimeoutSec;
        public const string AssociationTimeoutSec     = SharedNodeSettingKeys.Dicom.AssociationTimeoutSec;
        public const string DimseTimeoutSec           = SharedNodeSettingKeys.Dicom.DimseTimeoutSec;
        public const string MaxPduLength              = SharedNodeSettingKeys.Dicom.MaxPduLength;
        public const string MwlEnabled                = SharedNodeSettingKeys.Dicom.MwlEnabled;
        public const string CEchoEnabled              = SharedNodeSettingKeys.Dicom.CEchoEnabled;
        public const string QrEnabled                 = SharedNodeSettingKeys.Dicom.QrEnabled;
    }

    // ── Diagnostics
    public static class Diagnostics
    {
        public const string AssocLogEnabled    = SharedNodeSettingKeys.Diagnostics.AssocLogEnabled;
        public const string AssocLogLevel      = SharedNodeSettingKeys.Diagnostics.AssocLogLevel;
        public const string AssocLogRetainDays = SharedNodeSettingKeys.Diagnostics.AssocLogRetainDays;
    }

    // ── Cleanup
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

    // ── Transfer ──────────────────────────────────────────────────────────────────
    public static class Transfer
    {
        public const string MaxRetries        = SharedNodeSettingKeys.Transfer.MaxRetries;
        public const string RetryBaseDelaySec = SharedNodeSettingKeys.Transfer.RetryBaseDelaySec;
        public const string TimeoutSeconds    = SharedNodeSettingKeys.Transfer.TimeoutSeconds;
        public const string MaxConcurrent     = SharedNodeSettingKeys.Transfer.MaxConcurrent;
    }

    // ── Security ──────────────────────────────────────────────────────────────────
    public static class Security
    {
        public const string RequireTls         = SharedNodeSettingKeys.Security.RequireTls;
        public const string AuditRetentionDays = SharedNodeSettingKeys.Security.AuditRetentionDays;
    }

    // ── Storage ───────────────────────────────────────────────────────────────────
    public static class Storage
    {
        public const string RootPath    = SharedNodeSettingKeys.Storage.RootPath;
        public const string ArchivePath = SharedNodeSettingKeys.Storage.ArchivePath;
    }

    // ── PACS Sender ──────────────────────────────────────────────────────────────
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

    // ── PACS C-ECHO ──────────────────────────────────────────────────────────────
    public static class PacsCEcho
    {
        public const string Enabled         = SharedNodeSettingKeys.PacsCEcho.Enabled;
        public const string IntervalSeconds = SharedNodeSettingKeys.PacsCEcho.IntervalSeconds;
        public const string Destinations    = SharedNodeSettingKeys.PacsCEcho.Destinations;
    }

    // ── Node API ─────────────────────────────────────────────────────────────────
    public static class NodeApi
    {
        public const string Port = SharedNodeSettingKeys.NodeApi.Port;
    }

    // ── System (config sync metadata) ────────────────────────────────────────────
    public static class System
    {
        public const string ConfigVersion        = SharedNodeSettingKeys.System.ConfigVersion;
        public const string LastConfigAppliedUtc = SharedNodeSettingKeys.System.LastConfigAppliedUtc;
        public const string LastConfigSource     = SharedNodeSettingKeys.System.LastConfigSource;
    }
}
