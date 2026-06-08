using System.Net.Http.Json;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.Equipment;
using Dicom.Edge.Hub.Domain.Aggregates.Equipment;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Pushes the full equipment catalog (with modality codes) for a node immediately after
/// any CRUD operation. Calls POST /api/equipment/sync on the node.
/// </summary>
public sealed class NodeEquipmentPushService(
    IHttpClientFactory httpClientFactory,
    INodeRepository nodeRepository,
    INodeEquipmentRepository equipmentRepository,
    ILogger<NodeEquipmentPushService> logger) : INodeEquipmentPushService
{
    public async Task<bool> PushAsync(string nodeId, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null || string.IsNullOrWhiteSpace(node.ApiEndpoint))
        {
            logger.LogWarning(
                "Equipment push skipped: node {NodeId} not found or has no API endpoint", nodeId);
            return false;
        }

        var equipment = await equipmentRepository.GetByNodeIdAsync(nodeId, ct);

        var payload = new EquipmentSyncRequest
        {
            NodeId      = nodeId,
            SyncedAtUtc = DateTime.UtcNow,
            Equipment   = equipment.Select(e => new EquipmentSyncEntry
            {
                Id             = e.Id,
                AeTitle        = e.AeTitle,
                DisplayName    = e.DisplayName,
                ModalityCodes  = e.ModalityCodes,
                StationAeTitle = e.StationAeTitle,
                StationName    = e.StationName,
                IpAddress      = e.IpAddress,
                IsEnabled      = e.IsEnabled,
            }).ToList().AsReadOnly(),
        };

        var url = node.ApiEndpoint.TrimEnd('/') + "/api/equipment/sync";

        try
        {
            var client   = httpClientFactory.CreateClient(DispatchConstants.HttpClientName);
            var response = await client.PostAsJsonAsync(url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "Equipment push to node {NodeId} returned {Status}: {Body}",
                    nodeId, (int)response.StatusCode, body);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<EquipmentSyncResponse>(ct);

            logger.LogInformation(
                "Equipment pushed to node {NodeId}: {Applied} applied, {Removed} removed",
                nodeId, result?.AppliedCount ?? 0, result?.RemovedCount ?? 0);

            return result?.Accepted ?? false;
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("Equipment push to node {NodeId} timed out", nodeId);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex,
                "Equipment push to node {NodeId} failed: {Error}", nodeId, ex.Message);
            return false;
        }
    }
}
