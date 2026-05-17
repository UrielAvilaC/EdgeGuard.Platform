using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;

namespace Dicom.Edge.Hub.Application.Edge;

public interface IBootstrapTokenService
{
    /// <summary>Generates a new one-time bootstrap token and persists its hash.</summary>
    Task<BootstrapTokenResponse> GenerateAsync(
        CreateBootstrapTokenRequest request,
        string? createdByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates the raw token against the DB hash.
    /// Returns the token entity if valid; null otherwise.
    /// </summary>
    Task<NodeBootstrapToken?> ValidateAsync(string rawToken, CancellationToken ct = default);

    /// <summary>Marks the token as consumed after successful node registration.</summary>
    Task ConsumeAsync(NodeBootstrapToken token, string nodeId, CancellationToken ct = default);
}
