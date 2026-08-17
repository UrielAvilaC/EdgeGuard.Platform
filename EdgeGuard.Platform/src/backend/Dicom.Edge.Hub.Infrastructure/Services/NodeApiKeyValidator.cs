using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Security.Authentication;
using Dicom.Edge.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Validates Edge Node API keys against BCrypt hashes stored in the Node entity.
///
/// <para>Also the backfill point for <see cref="Node.SigningSecret"/>: nodes that registered
/// before that field existed have only a hash on the Hub, and a hash cannot be turned back into
/// the key needed to sign outbound pushes. Re-registration is not an option either — a node that
/// already holds a key never registers again. This is the one place where the Hub legitimately
/// sees the raw key, so it is captured here on the node's next authenticated call.</para>
/// </summary>
public sealed class NodeApiKeyValidator(
    INodeRepository nodeRepository,
    IPasswordHasher passwordHasher,
    ISettingEncryptionService secretProtector,
    IUnitOfWork unitOfWork,
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

                if (!MatchesStoredSecret(node, apiKey))
                    await BackfillSigningSecretAsync(node.Id, apiKey, ct);

                return new ApiKeyValidationResult(node.Id, node.Name, node.IsEnabled);
            }
        }

        return null;
    }

    /// <summary>
    /// True when the stored ciphertext still decrypts to the key the node just presented.
    /// A false answer covers three cases that all want the same repair: no secret stored yet,
    /// a stale secret from before a key rotation, and ciphertext that no longer decrypts
    /// because the Data Protection key ring was rotated or lost.
    /// </summary>
    private bool MatchesStoredSecret(Node node, string apiKey)
    {
        if (node.SigningSecret is not { Length: > 0 } encrypted) return false;

        try
        {
            return string.Equals(secretProtector.Decrypt(encrypted), apiKey, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Writes the encrypted form of the key the node just presented, whether it was missing
    /// or unusable. Never throws: authentication has already succeeded at this point, and
    /// failing to store the secret must not turn a valid call into a rejected one — the next
    /// call retries.
    /// </summary>
    private async Task BackfillSigningSecretAsync(string nodeId, string apiKey, CancellationToken ct)
    {
        try
        {
            // The matched instance came from a no-tracking query; reload it tracked so the
            // save touches this column only and respects the UpdatedAt concurrency token.
            var tracked = await nodeRepository.GetByIdAsync(nodeId, ct);
            if (tracked is null) return;

            tracked.SetSigningSecret(secretProtector.Encrypt(apiKey));
            await nodeRepository.UpdateAsync(tracked, ct);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Signing secret backfilled for node {NodeId} — Hub→Node requests can now be signed",
                nodeId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Could not backfill the signing secret for node {NodeId} — will retry on its next call",
                nodeId);
        }
    }
}
