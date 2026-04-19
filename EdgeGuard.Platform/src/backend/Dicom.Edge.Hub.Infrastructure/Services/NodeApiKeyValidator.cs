using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Security.Authentication;
using Dicom.Edge.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Validates Edge Node API keys against BCrypt hashes stored in the Node entity.
/// </summary>
public sealed class NodeApiKeyValidator(
    INodeRepository nodeRepository,
    IPasswordHasher passwordHasher,
    ILogger<NodeApiKeyValidator> logger) : IApiKeyValidator
{
    public async Task<ApiKeyValidationResult?> ValidateAsync(string apiKey, CancellationToken ct = default)
    {
        var nodes = await nodeRepository.GetNodesWithApiKeyAsync(ct);

        foreach (var node in nodes)
        {
            if (node.ApiKeyHash is null) continue;

            if (passwordHasher.VerifyPassword(apiKey, node.ApiKeyHash))
            {
                logger.LogDebug("API key matched node {NodeId}", node.Id);
                return new ApiKeyValidationResult(node.Id, node.Name, node.IsEnabled);
            }
        }

        return null;
    }
}
