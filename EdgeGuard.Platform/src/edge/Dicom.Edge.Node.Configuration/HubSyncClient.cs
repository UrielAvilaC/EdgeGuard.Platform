using System.Net.Http.Json;
using Dicom.Edge.Contracts.Edge;
using Dicom.Edge.Contracts.Hub;
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
    private string? _registeredNodeId;

    public async Task<bool> RegisterAsync(CancellationToken ct = default)
    {
        try
        {
            var payload = new NodeRegistrationRequest
            {
                Name = _opts.NodeName,
                AeTitle = _opts.AeTitle,
                IpAddress = _opts.IpAddress,
                Port = _opts.Port,
                ApiEndpoint = _opts.ApiEndpoint,
                Location = _opts.Location,
                FacilityName = _opts.FacilityName,
                Version = _opts.Version
            };

            var response = await httpClient.PostAsJsonAsync(
                HubApiRoutes.Register, payload, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<NodeRegistrationResponse>(ct);
                _registeredNodeId = body?.NodeId;
                logger.LogInformation("Registered with Hub successfully (NodeId={NodeId})", _registeredNodeId);
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
            if (string.IsNullOrEmpty(_registeredNodeId))
            {
                logger.LogDebug("Skipping heartbeat — node not yet registered");
                return false;
            }

            var payload = new NodeHeartbeatRequest
            {
                NodeId = _registeredNodeId
            };

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
            if (string.IsNullOrEmpty(_registeredNodeId))
            {
                logger.LogDebug("Skipping config pull — node not yet registered");
                return null;
            }

            var url = $"{HubApiRoutes.ConfigurationPull}?nodeId={Uri.EscapeDataString(_registeredNodeId)}";
            var response = await httpClient.GetAsync(url, ct);
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
