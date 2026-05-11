using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes;

/// <summary>
/// A one-time bootstrap token that authorises a single node registration.
/// The raw token is returned once to the admin; only the SHA-256 hash is persisted.
/// </summary>
public sealed class NodeBootstrapToken : Entity<string>
{
    /// <summary>SHA-256 hex hash of the raw token.</summary>
    public string TokenHash { get; private set; } = default!;

    public DateTime ExpiresAt { get; private set; }

    public bool IsConsumed { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    /// <summary>NodeId assigned after the registration that consumed this token.</summary>
    public string? ConsumedByNodeId { get; private set; }

    public string? CreatedByUserId { get; private set; }
    public string? Note { get; private set; }

    public bool IsExpired  => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid    => !IsConsumed && !IsExpired;

    private NodeBootstrapToken() { }

    public static NodeBootstrapToken Create(
        string tokenHash,
        DateTime expiresAt,
        string? createdByUserId = null,
        string? note = null)
    {
        return new NodeBootstrapToken
        {
            Id               = IdGenerator.NewId(),
            TokenHash        = tokenHash,
            ExpiresAt        = expiresAt,
            IsConsumed       = false,
            CreatedByUserId  = createdByUserId,
            Note             = note,
        };
    }

    /// <summary>Marks the token as consumed. Idempotent — safe to call multiple times.</summary>
    public void Consume(string nodeId)
    {
        if (IsConsumed) return;
        IsConsumed       = true;
        ConsumedAt       = DateTime.UtcNow;
        ConsumedByNodeId = nodeId;
    }
}
