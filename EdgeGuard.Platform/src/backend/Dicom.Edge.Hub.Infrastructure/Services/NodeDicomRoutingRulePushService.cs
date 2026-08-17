using System.Net.Http.Json;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.Routing;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Dicom.Edge.Hub.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Pushes the full set of Hub-managed DICOM routing rules for a node
/// immediately after any CRUD operation. Calls POST /api/dicom-routing-rules/sync.
/// </summary>
public sealed class NodeDicomRoutingRulePushService(
    IHttpClientFactory httpClientFactory,
    INodeRepository nodeRepository,
    INodeDicomRoutingRuleRepository ruleRepository,
    ILogger<NodeDicomRoutingRulePushService> logger) : INodeDicomRoutingRulePushService
{
    public async Task<bool> PushAsync(string nodeId, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null || string.IsNullOrWhiteSpace(node.ApiEndpoint))
        {
            logger.LogWarning(
                "DICOM routing rule push skipped: node {NodeId} not found or has no API endpoint", nodeId);
            return false;
        }

        var rules = await ruleRepository.GetByNodeIdAsync(nodeId, ct);

        var payload = new DicomRoutingRulesSyncRequest
        {
            NodeId      = nodeId,
            SyncedAtUtc = DateTime.UtcNow,
            Rules       = rules.Select(r => new DicomRoutingRuleSyncEntry
            {
                Id                      = r.Id,
                Name                    = r.Name,
                Priority                = r.Priority,
                IsEnabled               = r.IsEnabled,
                MatchModality           = r.MatchModality,
                MatchSourceAeTitle      = r.MatchSourceAeTitle,
                MatchInstitution        = r.MatchInstitution,
                MatchStudyDescContains  = r.MatchStudyDesc,
                MinInstanceCount        = r.MinInstanceCount,
                MaxInstanceCount        = r.MaxInstanceCount,
                DestinationAeTitle      = r.DestinationAeTitle,
                SendToPacs              = r.SendToPacs,
                SendToHub               = r.SendToHub,
                AnonymizeBeforeSending  = r.AnonymizeBeforeSend,
            }).ToList().AsReadOnly(),
        };

        var url = node.ApiEndpoint.TrimEnd('/') + "/api/dicom-routing-rules/sync";

        try
        {
            var client   = httpClientFactory.CreateClient(DispatchConstants.HttpClientName);
            var response = await client.PostAsJsonToNodeAsync(nodeId, url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "DICOM routing rule push to node {NodeId} returned {Status}: {Body}",
                    nodeId, (int)response.StatusCode, body);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<DicomRoutingRulesSyncResponse>(ct);

            logger.LogInformation(
                "DICOM routing rules pushed to node {NodeId}: {Applied} applied, {Removed} removed",
                nodeId, result?.AppliedCount ?? 0, result?.RemovedCount ?? 0);

            return result?.Accepted ?? false;
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("DICOM routing rule push to node {NodeId} timed out", nodeId);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex,
                "DICOM routing rule push to node {NodeId} failed: {Error}", nodeId, ex.Message);
            return false;
        }
    }
}
