namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Options for the Hub connectivity and configuration sync.
/// </summary>
public sealed class HubConnectionOptions
{
    public const string SectionName = "HubConnection";

    public bool Enabled { get; set; } = false;
    public string HubBaseUrl { get; set; } = "http://localhost:5000";
    public string ApiKey { get; set; } = string.Empty;
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
    public int Port { get; set; } = 11112;
    public string? ApiEndpoint { get; set; }
    public string? Location { get; set; }
    public string? FacilityName { get; set; }
    public string? Version { get; set; }
}
