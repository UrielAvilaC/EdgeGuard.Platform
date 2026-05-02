using System.Diagnostics;
using System.Net.Http.Json;
using Dicom.Edge.Contracts.Edge;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Security.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Dicom.Edge.Node.Configuration;

public sealed class HubSyncClient(
    HttpClient httpClient,
    IOptionsMonitor<HubConnectionOptions> optionsMonitor,
    ILogger<HubSyncClient> logger) : IHubSyncClient
{
    private HubConnectionOptions _opts => optionsMonitor.CurrentValue;
    private string? _registeredNodeId;
    private string? _apiKey;

    internal void SetApiKey(string apiKey) => _apiKey = apiKey;

    public async Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var resolvedApiEndpoint = !string.IsNullOrWhiteSpace(_opts.ApiEndpoint)
            ? _opts.ApiEndpoint
            : $"http://{_opts.IpAddress}:{_opts.ApiPort}/api";
        var registerUrl = $"{_opts.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.Register}";
        logger.LogInformation(
            "Hub registration starting -- HubBaseUrl={HubBaseUrl} RegisterUrl={RegisterUrl} AeTitle={AeTitle} IpAddress={IpAddress} DicomPort={DicomPort} ApiEndpoint={ApiEndpoint} NodeName={NodeName} FacilityName={FacilityName}",
            _opts.HubBaseUrl, registerUrl, _opts.AeTitle, _opts.IpAddress, _opts.Port, resolvedApiEndpoint, _opts.NodeName, _opts.FacilityName);
        try
        {
            var payload = new NodeRegistrationRequest
            {
                Name         = !string.IsNullOrWhiteSpace(_opts.NodeName) ? _opts.NodeName : Environment.MachineName,
                AeTitle      = _opts.AeTitle,
                IpAddress    = _opts.IpAddress,
                Port         = _opts.Port,
                ApiEndpoint  = resolvedApiEndpoint,
                Location     = _opts.Location,
                FacilityName = _opts.FacilityName,
                Version      = _opts.Version
            };
            var bootstrapToken = !string.IsNullOrWhiteSpace(_opts.BootstrapToken)
                ? _opts.BootstrapToken
                : await RequestBootstrapTokenAsync(ct);
            logger.LogDebug("Bootstrap token resolved -- Source={Source} HasToken={HasToken}",
                !string.IsNullOrWhiteSpace(_opts.BootstrapToken) ? "config" : "self-service", bootstrapToken is not null);
            using var request = new HttpRequestMessage(HttpMethod.Post, registerUrl);
            request.Content = JsonContent.Create(payload);
            if (!string.IsNullOrEmpty(bootstrapToken))
                request.Headers.Add(ApiKeyAuthenticationOptions.BootstrapHeaderName, bootstrapToken);
            var response = await httpClient.SendAsync(request, ct);
            logger.LogInformation("Hub register response -- StatusCode={StatusCode} ElapsedMs={ElapsedMs}",
                (int)response.StatusCode, sw.ElapsedMilliseconds);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<NodeRegistrationResponse>(ct);
                _registeredNodeId = body?.NodeId;
                if (!string.IsNullOrEmpty(body?.ApiKey))
                {
                    _apiKey = body.ApiKey;
                    logger.LogInformation("Registered with Hub -- NodeId={NodeId} ElapsedMs={ElapsedMs}", _registeredNodeId, sw.ElapsedMilliseconds);
                    return new RegistrationResult(true, body.ApiKey, body.NodeId);
                }
                logger.LogInformation("Re-registered with Hub -- NodeId={NodeId} ElapsedMs={ElapsedMs}", _registeredNodeId, sw.ElapsedMilliseconds);
                return new RegistrationResult(true, null, body?.NodeId);
            }
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Hub registration failed -- StatusCode={StatusCode} Response={ResponseBody} ElapsedMs={ElapsedMs}",
                (int)response.StatusCode, errorBody, sw.ElapsedMilliseconds);
            return new RegistrationResult(false, null, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hub registration error -- HubBaseUrl={HubBaseUrl} RegisterUrl={RegisterUrl} ElapsedMs={ElapsedMs}",
                _opts.HubBaseUrl, registerUrl, sw.ElapsedMilliseconds);
            return new RegistrationResult(false, null, null);
        }
    }

    private async Task<string?> RequestBootstrapTokenAsync(CancellationToken ct)
    {
        var tokenUrl = $"{_opts.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.RequestToken}";
        logger.LogDebug("Requesting bootstrap token -- HubBaseUrl={HubBaseUrl} TokenRoute={TokenRoute} ResolvedUrl={ResolvedUrl}",
            _opts.HubBaseUrl, HubApiRoutes.RequestToken, tokenUrl);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            var response = await httpClient.SendAsync(request, ct);
            logger.LogDebug("Bootstrap token response -- StatusCode={StatusCode} Url={Url}", (int)response.StatusCode, tokenUrl);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Failed to obtain bootstrap token -- StatusCode={StatusCode} Url={Url} Response={ResponseBody}",
                    (int)response.StatusCode, tokenUrl, body);
                return null;
            }
            var tokenBody = await response.Content.ReadFromJsonAsync<BootstrapTokenResponse>(ct);
            logger.LogInformation("Bootstrap token obtained -- ExpiresAt={ExpiresAt} Url={Url}", tokenBody?.ExpiresAt, tokenUrl);
            return tokenBody?.Token;
        }
        catch (UriFormatException ex)
        {
            logger.LogError(ex,
                "Invalid URI for bootstrap token -- HubBaseUrl={HubBaseUrl} TokenRoute={TokenRoute} ResolvedUrl={ResolvedUrl} -- Check HubConnection:HubBaseUrl in appsettings",
                _opts.HubBaseUrl, HubApiRoutes.RequestToken, tokenUrl);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not obtain bootstrap token -- HubBaseUrl={HubBaseUrl} Url={Url}", _opts.HubBaseUrl, tokenUrl);
            return null;
        }
    }

    public async Task<bool> SendHeartbeatAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_registeredNodeId)) { logger.LogDebug("Skipping heartbeat -- node not yet registered"); return false; }
        var heartbeatUrl = $"{_opts.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.Heartbeat}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, heartbeatUrl);
            request.Content = JsonContent.Create(new NodeHeartbeatRequest { NodeId = _registeredNodeId });
            ApplyApiKeyHeader(request);
            var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Heartbeat failed -- StatusCode={StatusCode} NodeId={NodeId}", (int)response.StatusCode, _registeredNodeId);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Heartbeat error -- NodeId={NodeId} Url={Url}", _registeredNodeId, heartbeatUrl);
            return false;
        }
    }

    public async Task<IReadOnlyDictionary<string, string>?> PullConfigurationAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_registeredNodeId)) { logger.LogDebug("Skipping config pull -- node not yet registered"); return null; }
        var configUrl = $"{_opts.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.ConfigurationPull}?nodeId={Uri.EscapeDataString(_registeredNodeId)}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, configUrl);
            ApplyApiKeyHeader(request);
            var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Config pull failed -- StatusCode={StatusCode} NodeId={NodeId} Url={Url}",
                    (int)response.StatusCode, _registeredNodeId, configUrl);
                return null;
            }
            var config = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(ct);
            logger.LogDebug("Config pull succeeded -- Count={Count} NodeId={NodeId}", config?.Count ?? 0, _registeredNodeId);
            return config;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Config pull error -- NodeId={NodeId} Url={Url}", _registeredNodeId, configUrl);
            return null;
        }
    }

    public async Task<bool> DeregisterAsync(CancellationToken ct = default)
    {
        var deregisterUrl = $"{_opts.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.Deregister}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, deregisterUrl);
            ApplyApiKeyHeader(request);
            var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Deregistration failed -- StatusCode={StatusCode} NodeId={NodeId}", (int)response.StatusCode, _registeredNodeId);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deregistration error -- NodeId={NodeId} Url={Url}", _registeredNodeId, deregisterUrl);
            return false;
        }
    }

    private void ApplyApiKeyHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_apiKey))
            request.Headers.Add(ApiKeyAuthenticationOptions.HeaderName, _apiKey);
    }
}
