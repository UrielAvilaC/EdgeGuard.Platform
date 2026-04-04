using System.Net.Http.Json;
using Dicom.Edge.Contracts.Edge;
using Dicom.Edge.Contracts.Hl7;
using Dicom.Edge.Hub.Application.Dispatch;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

public sealed class NodeHttpDispatcher : INodeDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NodeHttpDispatcher> _logger;

    public NodeHttpDispatcher(
        IHttpClientFactory httpClientFactory,
        ILogger<NodeHttpDispatcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<NodeDispatchResult> DispatchAsync(NodeDispatchRequest request, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(DispatchConstants.HttpClientName);

            var payload = new Hl7WorklistPushRequest
            {
                HubMessageId = request.MessageId,
                MessageType = request.MessageType,
                TriggerEvent = request.TriggerEvent,
                RawContent = request.Content,
                PatientId = request.PatientId,
                PatientName = request.PatientName,
                AccessionNumber = request.AccessionNumber,
                SendingFacility = request.SendingFacility,
                SendingApplication = request.SendingApplication,
                Priority = request.Priority,
                SentAtUtc = DateTime.UtcNow
            };

            var url = request.NodeApiEndpoint.TrimEnd('/') + NodeApiRoutes.Hl7WorklistPush;

            _logger.LogInformation(
                "Dispatching message {MessageId} to node {NodeId} at {Url}",
                request.MessageId, request.TargetNodeId, url);

            var response = await client.PostAsJsonAsync(url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Node {NodeId} returned {StatusCode} for message {MessageId}: {Body}",
                    request.TargetNodeId, response.StatusCode, request.MessageId, body);
                return NodeDispatchResult.Fail(
                    string.Format(DispatchConstants.HttpErrorTemplate, (int)response.StatusCode, body));
            }

            var ack = await response.Content.ReadFromJsonAsync<Hl7WorklistPushResponse>(ct);

            if (ack is null || !ack.Accepted)
            {
                var error = ack?.Error ?? DispatchConstants.NodeRejectedMessage;
                _logger.LogWarning(
                    "Node {NodeId} rejected message {MessageId}: {Error}",
                    request.TargetNodeId, request.MessageId, error);
                return NodeDispatchResult.Fail(error);
            }

            _logger.LogInformation(
                "Message {MessageId} delivered to node {NodeId}, AckId={AckId}",
                request.MessageId, request.TargetNodeId, ack.NodeAckId);

            return NodeDispatchResult.Ok(ack.NodeAckId);
        }
        catch (TaskCanceledException)
        {
            return NodeDispatchResult.Fail(DispatchConstants.DispatchTimeoutMessage);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error dispatching message {MessageId} to node {NodeId}",
                request.MessageId, request.TargetNodeId);
            return NodeDispatchResult.Fail(
                string.Format(DispatchConstants.ConnectionErrorTemplate, ex.Message));
        }
    }
}
