using System.Net.Http.Json;

namespace Dicom.Edge.Hub.Infrastructure.Http;

/// <summary>
/// Helpers for Hub→Node HTTP calls.
///
/// <para>Every request that targets a node must carry the <c>X-Node-Id</c> header:
/// <see cref="HubAuthDelegatingHandler"/> uses it to resolve that node's API key and sign
/// the request. <c>HttpClient.PostAsJsonAsync</c> gives no way to set headers per request,
/// so push services must go through these helpers instead — otherwise the request leaves
/// unsigned and the node logs <c>failed auth (missing-bearer)</c>.</para>
/// </summary>
public static class NodeHttpClientExtensions
{
    /// <summary>
    /// POSTs <paramref name="payload"/> as JSON to <paramref name="url"/>, tagging the request
    /// with the target node id so it gets signed.
    /// </summary>
    public static Task<HttpResponseMessage> PostAsJsonToNodeAsync<TPayload>(
        this HttpClient client,
        string nodeId,
        string url,
        TPayload payload,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add(HubAuthDelegatingHandler.NodeIdHeader, nodeId);

        return client.SendAsync(request, ct);
    }
}
