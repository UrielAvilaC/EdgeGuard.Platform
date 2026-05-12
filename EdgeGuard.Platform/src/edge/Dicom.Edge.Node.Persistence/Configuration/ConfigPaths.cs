using Dicom.Edge.Node.Persistence.Constants;

namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// Configuration path constants used by <see cref="NodeDatabaseConfigurationProvider"/>
/// to map database setting keys to <c>IConfiguration</c> section paths.
/// Section names come from <see cref="ConfigSectionNames"/> to keep a single source of truth.
/// </summary>
internal static class ConfigPaths
{
    // ── NodeApi ──────────────────────────────────────────────────────────────

    public const string NodeApiPort = $"{ConfigSectionNames.NodeApi}:Port";

    // ── HubConnection ────────────────────────────────────────────────────────

    public const string HubEnabled                  = $"{ConfigSectionNames.HubConnection}:Enabled";
    public const string HubApiKey                   = $"{ConfigSectionNames.HubConnection}:ApiKey";
    public const string HubNodeId                   = $"{ConfigSectionNames.HubConnection}:NodeId";
    public const string HubTimeoutSeconds           = $"{ConfigSectionNames.HubConnection}:TimeoutSeconds";
    public const string HubHeartbeatIntervalSeconds = $"{ConfigSectionNames.HubConnection}:HeartbeatIntervalSeconds";
    public const string HubRegisterOnStartup        = $"{ConfigSectionNames.HubConnection}:RegisterOnStartup";
    public const string HubMaxReconnectAttempts      = $"{ConfigSectionNames.HubConnection}:MaxReconnectAttempts";
    public const string HubReconnectDelaySeconds     = $"{ConfigSectionNames.HubConnection}:ReconnectDelaySeconds";
    public const string HubBaseUrl                  = $"{ConfigSectionNames.HubConnection}:HubBaseUrl";
    public const string HubConfigPullIntervalSeconds = $"{ConfigSectionNames.HubConnection}:ConfigPullIntervalSeconds";

    // ── HubConnection identity (General + Dicom keys → HubConnection section) ─

    public const string HubNodeName     = $"{ConfigSectionNames.HubConnection}:NodeName";
    public const string HubAeTitle      = $"{ConfigSectionNames.HubConnection}:AeTitle";
    public const string HubIpAddress    = $"{ConfigSectionNames.HubConnection}:IpAddress";
    public const string HubPort         = $"{ConfigSectionNames.HubConnection}:Port";
    public const string HubApiEndpoint  = $"{ConfigSectionNames.HubConnection}:ApiEndpoint";
    public const string HubLocation     = $"{ConfigSectionNames.HubConnection}:Location";
    public const string HubFacilityName = $"{ConfigSectionNames.HubConnection}:FacilityName";
    public const string HubVersion      = $"{ConfigSectionNames.HubConnection}:Version";

    // ── DicomServer ──────────────────────────────────────────────────────────

    public const string DicomEnabled              = $"{ConfigSectionNames.DicomServer}:Enabled";
    public const string DicomAeTitle              = $"{ConfigSectionNames.DicomServer}:AeTitle";
    public const string DicomPort                 = $"{ConfigSectionNames.DicomServer}:Port";
    public const string DicomMaxClients           = $"{ConfigSectionNames.DicomServer}:MaxClients";
    public const string DicomAssociationTimeout   = $"{ConfigSectionNames.DicomServer}:AssociationTimeoutSeconds";
    public const string DicomDimseTimeout         = $"{ConfigSectionNames.DicomServer}:DimseTimeoutSeconds";
    public const string DicomMaxPduLength         = $"{ConfigSectionNames.DicomServer}:MaxPduLength";
    public const string DicomMwlEnabled           = $"{ConfigSectionNames.DicomServer}:MwlEnabled";
    public const string DicomCEchoEnabled         = $"{ConfigSectionNames.DicomServer}:CEchoEnabled";
    public const string DicomQrEnabled            = $"{ConfigSectionNames.DicomServer}:QrEnabled";
    public const string DicomValidateCallingAe    = $"{ConfigSectionNames.DicomServer}:ValidateCallingAe";
    public const string DicomValidateCalledAe     = $"{ConfigSectionNames.DicomServer}:ValidateCalledAe";
    public const string DicomAllowedCallingPrefix = $"{ConfigSectionNames.DicomServer}:AllowedCallingAeTitles";
    public const string DicomAeTitleAliasesPrefix = $"{ConfigSectionNames.DicomServer}:AeTitleAliases";

    // ── PacsSender ───────────────────────────────────────────────────────────

    public const string PacsSenderEnabled                   = $"{ConfigSectionNames.PacsSender}:Enabled";
    public const string PacsSenderLocalAeTitle              = $"{ConfigSectionNames.PacsSender}:LocalAeTitle";
    public const string PacsSenderMaxConcurrentSends        = $"{ConfigSectionNames.PacsSender}:MaxConcurrentSends";
    public const string PacsSenderTimeoutSeconds            = $"{ConfigSectionNames.PacsSender}:TimeoutSeconds";
    public const string PacsSenderMaxRetries                = $"{ConfigSectionNames.PacsSender}:MaxRetries";
    public const string PacsSenderRetryBaseDelaySeconds     = $"{ConfigSectionNames.PacsSender}:RetryBaseDelaySeconds";
    public const string PacsSenderProcessingIntervalSeconds = $"{ConfigSectionNames.PacsSender}:ProcessingIntervalSeconds";

    // ── PacsCEcho ────────────────────────────────────────────────────────────

    public const string PacsCEchoEnabled         = $"{ConfigSectionNames.PacsCEcho}:Enabled";
    public const string PacsCEchoIntervalSeconds = $"{ConfigSectionNames.PacsCEcho}:IntervalSeconds";
    public const string PacsCEchoDestinations    = $"{ConfigSectionNames.PacsCEcho}:Destinations";

    // ── PacsDestination ──────────────────────────────────────────────────────

    public const string PacsDestinationHost    = $"{ConfigSectionNames.PacsDestination}:Host";
    public const string PacsDestinationPort    = $"{ConfigSectionNames.PacsDestination}:Port";
    public const string PacsDestinationAeTitle = $"{ConfigSectionNames.PacsDestination}:AeTitle";
}
