namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Options for the Hub connectivity and configuration sync.
/// </summary>
public sealed class HubConnectionOptions
{
    public const string SectionName = "HubConnection";

    public bool Enabled { get; set; } = true;
    public string HubBaseUrl { get; set; } = "http://localhost:5000";
    public string ApiKey { get; set; } = string.Empty;
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public int ConfigPullIntervalSeconds { get; set; } = 300;
    public int TimeoutSeconds { get; set; } = 30;
    public bool RegisterOnStartup { get; set; } = true;
    public int MaxReconnectAttempts { get; set; } = 10;
    public int ReconnectDelaySeconds { get; set; } = 30;
}
