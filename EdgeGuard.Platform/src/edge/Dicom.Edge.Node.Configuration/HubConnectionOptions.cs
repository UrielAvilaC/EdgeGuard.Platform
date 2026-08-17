namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Options for the Hub connectivity and configuration sync.
/// </summary>
public sealed class HubConnectionOptions
{
    public const string SectionName = "HubConnection";

    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Absolute http/https base URL of the Hub. No default on purpose: the deployed
    /// configuration (appsettings / environment variables) is the source of truth, and a
    /// built-in fallback would let a misconfigured node start and dial an endpoint nobody
    /// chose. Validated by <see cref="HubConnectionOptionsValidator"/> when
    /// <see cref="Enabled"/> is true.
    /// </summary>
    public string HubBaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string BootstrapToken { get; set; } = string.Empty;
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public int ConfigPullIntervalSeconds { get; set; } = 300;
    public int TimeoutSeconds { get; set; } = 30;
    public bool RegisterOnStartup { get; set; } = true;
    public int MaxReconnectAttempts { get; set; } = 10;
    public int ReconnectDelaySeconds { get; set; } = 30;

    // ── Node identity (sent during registration) ─────────────────────────
    public string NodeName { get; set; } = Environment.MachineName;
    public string AeTitle { get; set; } = "EDGE_NODE";
    public string IpAddress { get; set; } = "127.0.0.1";
    /// <summary>DICOM server port (C-STORE, C-ECHO, MWL). Sent to Hub during registration.</summary>
    public int Port { get; set; } = 11112;
    /// <summary>
    /// HTTP API port of this node. Used to build ApiEndpoint when not explicitly configured.
    /// When 0 (default), it is auto-filled from <c>NodeApi:Port</c> at startup.
    /// </summary>
    public int ApiPort { get; set; } = 0;
    public string? ApiEndpoint { get; set; }
    public string? Location { get; set; }
    public string? FacilityName { get; set; }
    public string? Version { get; set; }
}
