using System.Net.Http.Json;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Contracts.Edge;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Dicom.Edge.Hub.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Pushes active PACS destinations to an Edge Node immediately after any
/// assign/unassign operation. Calls <c>POST /api/pacs-destinations/sync</c>.
/// </summary>
public sealed class NodePacsDestinationPushService(
    IHttpClientFactory httpClientFactory,
    INodeRepository nodeRepository,
    IPacsServerRepository pacsRepository,
    ILogger<NodePacsDestinationPushService> logger) : INodePacsDestinationPushService
{
    public async Task<bool> PushAsync(string nodeId, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetWithPacsAssignmentsAsync(nodeId, ct);
        if (node is null || string.IsNullOrWhiteSpace(node.ApiEndpoint))
        {
            logger.LogWarning(
                "PACS push skipped: node {NodeId} not found or has no API endpoint", nodeId);
            return false;
        }

        // Resolve enabled PACS assigned to this node
        var activePacsIds = node.PacsAssignments
            .Where(a => a.IsActive)
            .Select(a => a.PacsId)
            .ToHashSet();

        var allPacs = await pacsRepository.GetAllAsync(ct);
        var destinations = allPacs
            .Where(p => p.IsEnabled && activePacsIds.Contains(p.Id))
            .Select((p, i) => new PacsDestinationSyncEntry
            {
                Id       = p.Id,
                Name     = p.Name,
                AeTitle  = p.AeTitle.Value,
                Host     = p.HostName,
                Port     = p.Port,
                Priority = (i + 1) * 10,
            })
            .ToList();

        var payload = new PacsDestinationsSyncRequest
        {
            NodeId       = nodeId,
            SyncedAtUtc  = DateTime.UtcNow,
            Destinations = destinations,
        };

        var url = node.ApiEndpoint.TrimEnd('/') + NodeApiRoutes.PacsDestinationsSync;

        try
        {
            var client = httpClientFactory.CreateClient(DispatchConstants.HttpClientName);
            var response = await client.PostAsJsonToNodeAsync(nodeId, url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "PACS push to node {NodeId} returned {Status}: {Body}",
                    nodeId, (int)response.StatusCode, body);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<PacsDestinationsSyncResponse>(ct);

            logger.LogInformation(
                "PACS destinations pushed to node {NodeId}: {Upserted} upserted, {Removed} removed",
                nodeId, result?.UpsertedCount ?? 0, result?.RemovedCount ?? 0);

            return result?.Accepted ?? false;
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("PACS push to node {NodeId} timed out", nodeId);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "PACS push to node {NodeId} failed: {Error}", nodeId, ex.Message);
            return false;
        }
    }
}
