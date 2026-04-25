using System.Security.Cryptography;
using System.Text;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Security.Authentication;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Edge;

/// <summary>
/// Manages one-time node bootstrap tokens.
/// Raw tokens are never persisted — only their SHA-256 hash is stored.
/// </summary>
public sealed class BootstrapTokenService(
    INodeBootstrapTokenRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<BootstrapTokenService> logger) : IBootstrapTokenService
{
    public async Task<BootstrapTokenResponse> GenerateAsync(
        CreateBootstrapTokenRequest request,
        string? createdByUserId,
        CancellationToken ct = default)
    {
        var rawToken  = ApiKeyGenerator.Generate();
        var hash      = ComputeHash(rawToken);
        // ExpiresInHours = 0 means short-lived self-service token (5 minutes)
        var expiresAt = request.ExpiresInHours > 0
            ? DateTime.UtcNow.AddHours(request.ExpiresInHours)
            : DateTime.UtcNow.AddMinutes(5);

        var entity = NodeBootstrapToken.Create(hash, expiresAt, createdByUserId, request.Note);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Bootstrap token {TokenId} issued by {User} — expires {ExpiresAt}",
            entity.Id, createdByUserId ?? "system", expiresAt);

        return new BootstrapTokenResponse
        {
            Token     = rawToken,
            TokenId   = entity.Id,
            ExpiresAt = expiresAt,
            Note      = request.Note,
        };
    }

    public async Task<NodeBootstrapToken?> ValidateAsync(string rawToken, CancellationToken ct = default)
    {
        var hash  = ComputeHash(rawToken);
        var token = await repository.GetByTokenHashAsync(hash, ct);

        if (token is null)
        {
            logger.LogWarning("Bootstrap token not found");
            return null;
        }

        if (!token.IsValid)
        {
            logger.LogWarning(
                "Bootstrap token {TokenId} rejected — consumed={C} expired={E}",
                token.Id, token.IsConsumed, token.IsExpired);
            return null;
        }

        return token;
    }

    public async Task ConsumeAsync(NodeBootstrapToken token, string nodeId, CancellationToken ct = default)
    {
        token.Consume(nodeId);
        await repository.UpdateAsync(token, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Bootstrap token {TokenId} consumed by node {NodeId}",
            token.Id, nodeId);
    }

    private static string ComputeHash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(bytes);
    }
}
