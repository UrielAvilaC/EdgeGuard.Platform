namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes;

/// <summary>
/// Repository interface for <see cref="NodeBootstrapToken"/>.
/// </summary>
public interface INodeBootstrapTokenRepository
{
    Task<NodeBootstrapToken> AddAsync(NodeBootstrapToken token, CancellationToken ct = default);
    Task<NodeBootstrapToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task UpdateAsync(NodeBootstrapToken token, CancellationToken ct = default);
    Task<IReadOnlyList<NodeBootstrapToken>> GetActiveAsync(CancellationToken ct = default);
}
