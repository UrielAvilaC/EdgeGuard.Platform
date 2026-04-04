using System.Net.Http.Json;
using Dicom.Edge.Contracts.Edge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// HTTP-based client for Hub registration, heartbeat, and configuration pull.
/// </summary>
public sealed class HubSyncClient(
    HttpClient httpClient,
    IOptions<HubConnectionOptions> options,
    ILogger<HubSyncClient> logger) : IHubSyncClient
{
    private readonly HubConnectionOptions _opts = options.Value;

    public async Task<bool> RegisterAsync(CancellationToken ct = default)
    {
        try
        {
            var payload = new { MachineName = Environment.MachineName, Timestamp = DateTime.UtcNow };
            var response = await httpClient.PostAsJsonAsync(
                HubApiRoutes.Register, payload, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Registered with Hub successfully");
                return true;
            }

            logger.LogWarning("Hub registration failed: {Status}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hub registration error");
            return false;
        }
    }

    public async Task<bool> SendHeartbeatAsync(CancellationToken ct = default)
    {
        try
        {
            var payload = new { Timestamp = DateTime.UtcNow, MachineName = Environment.MachineName };
            var response = await httpClient.PostAsJsonAsync(
                HubApiRoutes.Heartbeat, payload, ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Heartbeat failed");
            return false;
        }
    }

    public async Task<IReadOnlyDictionary<string, string>?> PullConfigurationAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(HubApiRoutes.ConfigurationPull, ct);
            if (!response.IsSuccessStatusCode) return null;

            var config = await response.Content
                .ReadFromJsonAsync<Dictionary<string, string>>(ct);
            logger.LogDebug("Pulled {Count} config entries from Hub", config?.Count ?? 0);
            return config;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Config pull failed");
            return null;
        }
    }

    public async Task<bool> DeregisterAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync(HubApiRoutes.Deregister, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deregistration failed");
            return false;
        }
    }
}
