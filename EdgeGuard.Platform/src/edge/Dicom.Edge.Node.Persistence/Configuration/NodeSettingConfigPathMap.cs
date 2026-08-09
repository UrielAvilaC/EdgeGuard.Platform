using Dicom.Edge.Node.Persistence.Constants;

namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// Maps <c>node_settings</c> DB keys to their corresponding <c>IConfiguration</c> paths.
/// Used by <see cref="Services.NodeSettingsService"/> to fall back to environment variables
/// or appsettings when a DB entry is empty, implementing the priority chain:
/// <b>DB → environment variables → appsettings</b>.
/// </summary>
internal static class NodeSettingConfigPathMap
{
    /// <summary>
    /// Returns the <c>IConfiguration</c> path for a given DB settings key,
    /// or <c>null</c> if the key has no corresponding configuration path.
    /// </summary>
    public static string? GetConfigPath(string dbKey) =>
        _map.TryGetValue(dbKey, out var path) ? path : null;

    private static readonly Dictionary<string, string> _map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ── Node API ─────────────────────────────────────────────────────
            [NodeSettingKeys.NodeApi.Port]                    = ConfigPaths.NodeApiPort,

            // ── Hub Connection ───────────────────────────────────────────────
            [NodeSettingKeys.Hub.ApiKey]                      = ConfigPaths.HubApiKey,
            [NodeSettingKeys.Hub.TimeoutSeconds]              = ConfigPaths.HubTimeoutSeconds,
            [NodeSettingKeys.Hub.HeartbeatIntervalSec]        = ConfigPaths.HubHeartbeatIntervalSeconds,
            [NodeSettingKeys.Hub.RegisterOnStartup]           = ConfigPaths.HubRegisterOnStartup,
            [NodeSettingKeys.Hub.MaxReconnectAttempts]        = ConfigPaths.HubMaxReconnectAttempts,
            [NodeSettingKeys.Hub.ReconnectDelaySeconds]       = ConfigPaths.HubReconnectDelaySeconds,
            [NodeSettingKeys.Hub.Hostname]                    = ConfigPaths.HubBaseUrl,

            // ── Hub identity (General → HubConnection) ───────────────────────
            [NodeSettingKeys.General.NodeName]                = ConfigPaths.HubNodeName,
            [NodeSettingKeys.General.AeTitle]                 = ConfigPaths.HubAeTitle,
            [NodeSettingKeys.General.IpAddress]               = ConfigPaths.HubIpAddress,
            [NodeSettingKeys.General.ApiEndpoint]             = ConfigPaths.HubApiEndpoint,
            [NodeSettingKeys.General.Location]                = ConfigPaths.HubLocation,
            [NodeSettingKeys.General.FacilityName]            = ConfigPaths.HubFacilityName,
            [NodeSettingKeys.General.Version]                 = ConfigPaths.HubVersion,

            // ── DICOM Server ─────────────────────────────────────────────────
            [NodeSettingKeys.Dicom.Enabled]                   = ConfigPaths.DicomEnabled,
            [NodeSettingKeys.Dicom.AeTitle]                   = ConfigPaths.DicomAeTitle,
            [NodeSettingKeys.Dicom.Port]                      = ConfigPaths.DicomPort,
            [NodeSettingKeys.Dicom.MaxAssociations]           = ConfigPaths.DicomMaxClients,
            [NodeSettingKeys.Dicom.AssociationTimeoutSec]     = ConfigPaths.DicomAssociationTimeout,
            [NodeSettingKeys.Dicom.DimseTimeoutSec]           = ConfigPaths.DicomDimseTimeout,
            [NodeSettingKeys.Dicom.MaxPduLength]              = ConfigPaths.DicomMaxPduLength,
            [NodeSettingKeys.Dicom.MwlEnabled]                = ConfigPaths.DicomMwlEnabled,
            [NodeSettingKeys.Dicom.CEchoEnabled]              = ConfigPaths.DicomCEchoEnabled,
            [NodeSettingKeys.Dicom.QrEnabled]                 = ConfigPaths.DicomQrEnabled,
            [NodeSettingKeys.Dicom.ValidateCallingAe]         = ConfigPaths.DicomValidateCallingAe,
            [NodeSettingKeys.Dicom.ValidateCalledAe]          = ConfigPaths.DicomValidateCalledAe,

            // ── Diagnostics (per-association logging) ────────────────────────
            [NodeSettingKeys.Diagnostics.AssocLogEnabled]     = ConfigPaths.AssocLogEnabled,
            [NodeSettingKeys.Diagnostics.AssocLogLevel]       = ConfigPaths.AssocLogLevel,
            [NodeSettingKeys.Diagnostics.AssocLogRetainDays]  = ConfigPaths.AssocLogRetainDays,

            // ── PACS Sender ──────────────────────────────────────────────────
            [NodeSettingKeys.PacsSender.Enabled]                   = ConfigPaths.PacsSenderEnabled,
            [NodeSettingKeys.PacsSender.LocalAeTitle]              = ConfigPaths.PacsSenderLocalAeTitle,
            [NodeSettingKeys.PacsSender.MaxConcurrentSends]        = ConfigPaths.PacsSenderMaxConcurrentSends,
            [NodeSettingKeys.PacsSender.TimeoutSeconds]            = ConfigPaths.PacsSenderTimeoutSeconds,
            [NodeSettingKeys.PacsSender.MaxRetries]                = ConfigPaths.PacsSenderMaxRetries,
            [NodeSettingKeys.PacsSender.RetryBaseDelaySeconds]     = ConfigPaths.PacsSenderRetryBaseDelaySeconds,
            [NodeSettingKeys.PacsSender.ProcessingIntervalSeconds] = ConfigPaths.PacsSenderProcessingIntervalSeconds,

            // ── PACS C-ECHO ──────────────────────────────────────────────────
            [NodeSettingKeys.PacsCEcho.Enabled]                    = ConfigPaths.PacsCEchoEnabled,
            [NodeSettingKeys.PacsCEcho.IntervalSeconds]            = ConfigPaths.PacsCEchoIntervalSeconds,
        };
}
