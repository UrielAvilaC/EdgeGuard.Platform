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
    public HubConnectionOptions ConnectionOptions => optionsMonitor.CurrentValue;
    private string? _registeredNodeId;
    private string? _apiKey;

    
    /// <inheritdoc />
    public string? RegisteredNodeId => _registeredNodeId ?? ConnectionOptions.NodeId;

    internal void SetApiKey(string apiKey) => _apiKey = apiKey;
    internal void SetRegisteredNodeId(string nodeId) => _registeredNodeId = nodeId;

    public async Task<RegistrationResult> RegisterAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var resolvedApiEndpoint = !string.IsNullOrWhiteSpace(ConnectionOptions.ApiEndpoint)
            ? ConnectionOptions.ApiEndpoint
            : $"http://{ConnectionOptions.IpAddress}:{ConnectionOptions.ApiPort}/api";
        var registerUrl = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.Register}";
        logger.LogInformation(
            "Hub registration starting -- HubBaseUrl={HubBaseUrl} RegisterUrl={RegisterUrl} AeTitle={AeTitle} IpAddress={IpAddress} DicomPort={DicomPort} ApiEndpoint={ApiEndpoint} NodeName={NodeName} FacilityName={FacilityName}",
            ConnectionOptions.HubBaseUrl, registerUrl, ConnectionOptions.AeTitle, ConnectionOptions.IpAddress, ConnectionOptions.Port, resolvedApiEndpoint, ConnectionOptions.NodeName, ConnectionOptions.FacilityName);
        try
        {
            var payload = new NodeRegistrationRequest
            {
                Name         = !string.IsNullOrWhiteSpace(ConnectionOptions.NodeName) ? ConnectionOptions.NodeName : Environment.MachineName,
                AeTitle      = ConnectionOptions.AeTitle,
                IpAddress    = ConnectionOptions.IpAddress,
                Port         = ConnectionOptions.Port,
                ApiEndpoint  = resolvedApiEndpoint,
                Location     = ConnectionOptions.Location,
                FacilityName = ConnectionOptions.FacilityName,
                Version      = ConnectionOptions.Version
            };
            var bootstrapToken = !string.IsNullOrWhiteSpace(ConnectionOptions.BootstrapToken)
                ? ConnectionOptions.BootstrapToken
                : await RequestBootstrapTokenAsync(ct);
            logger.LogDebug("Bootstrap token resolved -- Source={Source} HasToken={HasToken}",
                !string.IsNullOrWhiteSpace(ConnectionOptions.BootstrapToken) ? "config" : "self-service", bootstrapToken is not null);
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
                ConnectionOptions.HubBaseUrl, registerUrl, sw.ElapsedMilliseconds);
            return new RegistrationResult(false, null, null);
        }
    }

    private async Task<string?> RequestBootstrapTokenAsync(CancellationToken ct)
    {
        var tokenUrl = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.RequestToken}";
        logger.LogDebug("Requesting bootstrap token -- HubBaseUrl={HubBaseUrl} TokenRoute={TokenRoute} ResolvedUrl={ResolvedUrl}",
            ConnectionOptions.HubBaseUrl, HubApiRoutes.RequestToken, tokenUrl);
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
                ConnectionOptions.HubBaseUrl, HubApiRoutes.RequestToken, tokenUrl);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not obtain bootstrap token -- HubBaseUrl={HubBaseUrl} Url={Url}", ConnectionOptions.HubBaseUrl, tokenUrl);
            return null;
        }
    }

    public async Task<bool> SendHeartbeatAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_registeredNodeId)) { logger.LogDebug("Skipping heartbeat -- node not yet registered"); return false; }
        var heartbeatUrl = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.Heartbeat}";
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
        var configUrl = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.ConfigurationPull}?nodeId={Uri.EscapeDataString(_registeredNodeId)}";
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
        var deregisterUrl = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.Deregister}";
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

    public async Task<bool> SendTelemetryAsync(NodeTelemetryRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_registeredNodeId)) { logger.LogDebug("Skipping telemetry -- node not yet registered"); return false; }
        var url = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.Telemetry}";
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(request);
            ApplyApiKeyHeader(httpRequest);
            var response = await httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Telemetry send failed -- StatusCode={StatusCode} NodeId={NodeId}", (int)response.StatusCode, _registeredNodeId);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telemetry send error -- NodeId={NodeId} Url={Url}", _registeredNodeId, url);
            return false;
        }
    }

    public async Task<bool> NotifyStudyAsync(StudyNotifyRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Study notify requested -- StudyUid={StudyUid} PatientId={PatientId} PatientName={PatientName} AccessionNumber={AccessionNumber} InstanceCount={InstanceCount} TotalSizeBytes={TotalSizeBytes} NodeId={NodeId}",
            request.StudyInstanceUid, request.PatientId, request.PatientName, request.AccessionNumber, request.InstanceCount, request.TotalSizeBytes, _registeredNodeId??request.NodeId);

        
        
        logger.LogInformation("Sending study notify -- StudyUid={StudyUid} PatientId={PatientId} PatientName={PatientName} AccessionNumber={AccessionNumber} InstanceCount={InstanceCount} TotalSizeBytes={TotalSizeBytes}",
            request.StudyInstanceUid, request.PatientId, request.PatientName, request.AccessionNumber, request.InstanceCount, request.TotalSizeBytes);

        var url = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.StudyNotify}";
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(request);
            ApplyApiKeyHeader(httpRequest);
            logger.LogInformation("Sending study notify -- StudyUid={StudyUid} PatientId={PatientId} PatientName={PatientName} AccessionNumber={AccessionNumber} InstanceCount={InstanceCount} TotalSizeBytes={TotalSizeBytes}",
                request.StudyInstanceUid, request.PatientId, request.PatientName, request.AccessionNumber, request.InstanceCount, request.TotalSizeBytes);
            var response = await httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Study notify failed -- StatusCode={StatusCode} StudyUid={StudyUid}",
                    (int)response.StatusCode, request.StudyInstanceUid);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Study notify error -- StudyUid={StudyUid} Url={Url}",
                request.StudyInstanceUid, url);
            return false;
        }
    }

    public async Task<bool> NotifyStudyProgressAsync(StudyProgressNotifyRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_registeredNodeId)) { logger.LogDebug("Skipping study progress notify -- node not yet registered"); return false; }
        var url = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.StudyProgress}";
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(request);
            ApplyApiKeyHeader(httpRequest);
            var response = await httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Study progress notify failed -- StatusCode={StatusCode} StudyUid={StudyUid}",
                    (int)response.StatusCode, request.StudyInstanceUid);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Study progress notify error -- StudyUid={StudyUid} Url={Url}",
                request.StudyInstanceUid, url);
            return false;
        }
    }

    public async Task<bool> NotifyStudyPacsStatusAsync(StudyPacsStatusNotifyRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_registeredNodeId)) { logger.LogDebug("Skipping PACS status notify -- node not yet registered"); return false; }
        var url = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.StudyPacsStatus}";
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(request);
            ApplyApiKeyHeader(httpRequest);
            var response = await httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("PACS status notify failed -- StatusCode={StatusCode} StudyUid={StudyUid} Status={Status}",
                    (int)response.StatusCode, request.StudyInstanceUid, request.Status);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PACS status notify error -- StudyUid={StudyUid} Status={Status} Url={Url}",
                request.StudyInstanceUid, request.Status, url);
            return false;
        }
    }

    public async Task<bool> ReportPacsEchoAsync(NodePacsEchoReportRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(RegisteredNodeId)) { logger.LogDebug("Skipping PACS echo report -- node not yet registered"); return false; }
        var url = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.PacsEchoReport}";
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(request);
            ApplyApiKeyHeader(httpRequest);
            var response = await httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("PACS echo report failed -- StatusCode={StatusCode} NodeId={NodeId}",
                    (int)response.StatusCode, _registeredNodeId);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PACS echo report error -- NodeId={NodeId} Url={Url}", _registeredNodeId, url);
            return false;
        }
    }

    public async Task<bool> ReportEquipmentStatusAsync(NodeEquipmentStatusReportRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(RegisteredNodeId)) { logger.LogDebug("Skipping equipment status report -- node not yet registered"); return false; }
        var url = $"{ConnectionOptions.HubBaseUrl.TrimEnd('/')}{HubApiRoutes.EquipmentStatusReport}";
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = JsonContent.Create(request);
            ApplyApiKeyHeader(httpRequest);
            var response = await httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Equipment status report failed -- StatusCode={StatusCode} NodeId={NodeId}",
                    (int)response.StatusCode, _registeredNodeId);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Equipment status report error -- NodeId={NodeId} Url={Url}", _registeredNodeId, url);
            return false;
        }
    }

    private void ApplyApiKeyHeader(HttpRequestMessage request)
    {
        var apiKey = _apiKey ?? ConnectionOptions.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Add(ApiKeyAuthenticationOptions.HeaderName, apiKey);
    }
}
