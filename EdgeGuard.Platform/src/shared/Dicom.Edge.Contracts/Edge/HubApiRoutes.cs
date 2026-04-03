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
}
