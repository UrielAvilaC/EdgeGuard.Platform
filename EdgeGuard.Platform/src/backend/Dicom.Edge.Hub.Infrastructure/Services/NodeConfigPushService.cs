using System.Net.Http.Json;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Contracts.Edge;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Pushes a full configuration snapshot from the Hub to an Edge Node over HTTP.
/// Uses the same named HTTP client as <see cref="NodeHttpDispatcher"/>.
/// </summary>
public sealed class NodeConfigPushService : INodeConfigPushService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly INodeConfigurationService _configService;
    private readonly INodeRepository _nodeRepository;
    private readonly ILogger<NodeConfigPushService> _logger;

    public NodeConfigPushService(
        IHttpClientFactory httpClientFactory,
        INodeConfigurationService configService,
        INodeRepository nodeRepository,
        ILogger<NodeConfigPushService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _nodeRepository = nodeRepository;
        _logger = logger;
    }

    public async Task<ConfigPushResult> PushConfigAsync(
        string nodeId, CancellationToken ct = default)
    {
        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null || string.IsNullOrWhiteSpace(node.ApiEndpoint))
        {
            _logger.LogWarning("Cannot push config: node {NodeId} not found or has no API endpoint", nodeId);
            return ConfigPushResult.Fail(DispatchConstants.TargetNodeNotFoundMessage);
        }

        try
        {
            var syncDto = await _configService.BuildSyncPayloadAsync(nodeId, ct);
            var client = _httpClientFactory.CreateClient(DispatchConstants.HttpClientName);
            var url = node.ApiEndpoint.TrimEnd('/') + NodeApiRoutes.ConfigurationApply;

            _logger.LogInformation(
                "Pushing configuration to node {NodeId} at {Url}, version={Version}",
                nodeId, url, syncDto.ConfigVersion);

            var response = await client.PostAsJsonAsync(url, syncDto, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Node {NodeId} returned {StatusCode} during config push: {Body}",
                    nodeId, response.StatusCode, body);
                return ConfigPushResult.Fail(
                    string.Format(DispatchConstants.HttpErrorTemplate, (int)response.StatusCode, body));
            }

            var result = await response.Content.ReadFromJsonAsync<ConfigSyncResultDto>(ct);

            if (result is null || !result.Accepted)
            {
                var error = result?.Error ?? "Node rejected configuration without reason.";
                _logger.LogWarning("Node {NodeId} rejected config push: {Error}", nodeId, error);
                return ConfigPushResult.Fail(error);
            }

            _logger.LogInformation(
                "Configuration pushed to node {NodeId}. AppliedVersion={Version}, Updated={Count}",
                nodeId, result.AppliedVersion, result.UpdatedCount);

            return ConfigPushResult.Ok(result.AppliedVersion ?? syncDto.ConfigVersion, result.UpdatedCount);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Config push to node {NodeId} timed out", nodeId);
            return ConfigPushResult.Fail(DispatchConstants.DispatchTimeoutMessage);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error pushing config to node {NodeId}", nodeId);
            return ConfigPushResult.Fail(
                string.Format(DispatchConstants.ConnectionErrorTemplate, ex.Message));
        }
    }
}
