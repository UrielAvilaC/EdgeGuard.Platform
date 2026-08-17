using System.Net.Http.Json;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Contracts.Edge;
using Dicom.Edge.Hub.Application.Studies;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Dicom.Edge.Hub.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Pushes a manual study resend request to a node. Calls POST /api/studies/requeue on the node.
/// </summary>
public sealed class StudyRequeuePushService(
    IHttpClientFactory httpClientFactory,
    INodeRepository nodeRepository,
    ILogger<StudyRequeuePushService> logger) : IStudyRequeuePushService
{
    public async Task<bool> PushAsync(
        string nodeId, string studyInstanceUid, IReadOnlyList<string> pacsIds, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null || string.IsNullOrWhiteSpace(node.ApiEndpoint))
        {
            logger.LogWarning(
                "Study requeue push skipped: node {NodeId} not found or has no API endpoint", nodeId);
            return false;
        }

        var payload = new StudyRequeueRequest
        {
            StudyInstanceUid = studyInstanceUid,
            PacsIds = pacsIds,
        };

        var url = node.ApiEndpoint.TrimEnd('/') + NodeApiRoutes.StudiesRequeue;

        try
        {
            var client = httpClientFactory.CreateClient(DispatchConstants.HttpClientName);

            // /api/studies is a protected prefix on the node: the request must carry the
            // target node id so HubAuthDelegatingHandler can sign it. Without it the node
            // rejects (or, with NodeAuth:Enforce=false, logs) "missing-bearer".
            var response = await client.PostAsJsonToNodeAsync(nodeId, url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "Study requeue push for {StudyUid} to node {NodeId} returned {Status}: {Body}",
                    studyInstanceUid, nodeId, (int)response.StatusCode, body);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<StudyRequeueResponse>(ct);

            logger.LogInformation(
                "Study {StudyUid} requeue pushed to node {NodeId}: Accepted={Accepted}, Targets={Count}",
                studyInstanceUid, nodeId, result?.Accepted ?? false, result?.TargetCount ?? 0);

            return result?.Accepted ?? false;
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("Study requeue push for {StudyUid} to node {NodeId} timed out", studyInstanceUid, nodeId);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex,
                "Study requeue push for {StudyUid} to node {NodeId} failed: {Error}",
                studyInstanceUid, nodeId, ex.Message);
            return false;
        }
    }
}
