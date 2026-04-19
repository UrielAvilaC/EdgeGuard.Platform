using System.Net.Http.Json;
using Dicom.Edge.Contracts.Edge;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Security.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// HTTP-based client for Hub registration, heartbeat, and configuration pull.
/// Registration uses the bootstrap token (X-Bootstrap-Token header).
/// All other calls use the per-node API key (X-Api-Key header).
/// </summary>
public sealed class HubSyncClient(
    HttpClient httpClient,
    IOptions<HubConnectionOptions> options,
    ILogger<HubSyncClient> logger) : IHubSyncClient
{
    private readonly HubConnectionOptions _opts = options.Value;
    private string? _registeredNodeId;

    /// <summary>API key cached in-memory after registration or loaded from settings.</summary>
    private string? _apiKey;

    /// <summary>
    /// Sets the API key for subsequent calls. Called by the hosted service
    /// after loading from DB or receiving from registration.
    /// </summary>
    internal void SetApiKey(string apiKey) => _apiKey = apiKey;

    public async Task<string?> RegisterAsync(CancellationToken ct = default)
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

            // Use bootstrap token for registration
            var bootstrapToken = !string.IsNullOrEmpty(_opts.BootstrapToken)
                ? _opts.BootstrapToken
                : Environment.GetEnvironmentVariable(ApiKeyAuthenticationOptions.BootstrapTokenEnvVar);

            using var request = new HttpRequestMessage(HttpMethod.Post, HubApiRoutes.Register);
            request.Content = JsonContent.Create(payload);

            if (!string.IsNullOrEmpty(bootstrapToken))
                request.Headers.Add(ApiKeyAuthenticationOptions.BootstrapHeaderName, bootstrapToken);

            var response = await httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<NodeRegistrationResponse>(ct);
                _registeredNodeId = body?.NodeId;

                if (!string.IsNullOrEmpty(body?.ApiKey))
                {
                    _apiKey = body.ApiKey;
                    logger.LogInformation(
                        "Registered with Hub (NodeId={NodeId}) — API key received and will be persisted",
                        _registeredNodeId);
                    return body.ApiKey;
                }

                // Re-registration: API key unchanged, use existing
                logger.LogInformation(
                    "Re-registered with Hub (NodeId={NodeId}) — using existing API key",
                    _registeredNodeId);
                return null;
            }

            logger.LogWarning("Hub registration failed: {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hub registration error");
            return null;
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

            using var request = new HttpRequestMessage(HttpMethod.Post, HubApiRoutes.Heartbeat);
            request.Content = JsonContent.Create(payload);
            ApplyApiKeyHeader(request);

            var response = await httpClient.SendAsync(request, ct);
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
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyApiKeyHeader(request);

            var response = await httpClient.SendAsync(request, ct);
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
            using var request = new HttpRequestMessage(HttpMethod.Delete, HubApiRoutes.Deregister);
            ApplyApiKeyHeader(request);

            var response = await httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deregistration failed");
            return false;
        }
    }

    private void ApplyApiKeyHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_apiKey))
            request.Headers.Add(ApiKeyAuthenticationOptions.HeaderName, _apiKey);
    }
}
