namespace Dicom.Edge.Contracts.Edge;

/// <summary>
/// HTTP route constants for Hub ↔ Edge Node communication.
/// Shared by both the Hub API and the Edge Node HTTP client.
/// Prefix all paths with the Hub base URL: <c>{Protocol}://{Hostname}:{Port}{BasePath}</c>
/// </summary>
public static class HubApiRoutes
{
    /// <summary>POST — Node registration. Returns <c>NodeRegistrationResponse</c>.</summary>
    public const string Register = "/edge/register";

    /// <summary>DELETE — Node deregistration.</summary>
    public const string Deregister = "/edge/deregister";

    /// <summary>POST — Periodic heartbeat. Returns optional <c>HeartbeatResponse</c>.</summary>
    public const string Heartbeat = "/edge/heartbeat";

    /// <summary>GET — Pull latest configuration for this node. Returns <c>NodeConfigurationDto</c>.</summary>
    public const string ConfigurationPull = "/edge/configuration";

    /// <summary>POST — Notify Hub a study was received. Returns acknowledgment.</summary>
    public const string StudyNotify = "/edge/studies";

    /// <summary>POST — Report node health metrics. Returns acknowledgment.</summary>
    public const string HealthReport = "/edge/health";

    /// <summary>GET — Hub version and capability info.</summary>
    public const string HubInfo = "/info";

    /// <summary>
    /// POST — Request a self-service bootstrap token (no auth required).
    /// Returns a short-lived one-time token the node uses to call <see cref="Register"/>.
    /// </summary>
    public const string RequestToken = "/edge/token";
}

/// <summary>
/// HTTP route constants for Hub → Edge Node push communication.
/// The Edge Node exposes these endpoints and the Hub dispatches data to them.
/// </summary>
public static class NodeApiRoutes
{
    /// <summary>POST — Hub pushes an HL7 worklist item to the node.</summary>
    public const string Hl7WorklistPush = "/api/hl7/worklist";

    /// <summary>GET — Health check endpoint on the node.</summary>
    public const string HealthCheck = "/api/health";

    /// <summary>GET — Active worklist items on the node.</summary>
    public const string WorklistItems = "/api/dicom/worklist";

    /// <summary>GET — Active worklist item count.</summary>
    public const string WorklistCount = "/api/dicom/worklist/count";

    /// <summary>GET — PACS C-ECHO connectivity status.</summary>
    public const string PacsStatus = "/api/dicom/pacs/status";

    /// <summary>POST — Hub pushes a full configuration snapshot to the node.</summary>
    public const string ConfigurationApply = "/api/configuration/apply";

    /// <summary>GET — Returns the node's current config version hash.</summary>
    public const string ConfigurationVersion = "/api/configuration/version";
}
