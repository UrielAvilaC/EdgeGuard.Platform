namespace Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;

/// <summary>
/// Repository for <see cref="NodeConfigurationProfile"/> entities.
/// </summary>
public interface INodeConfigurationProfileRepository
{
    /// <summary>Returns all configuration entries for a given node.</summary>
    Task<IReadOnlyList<NodeConfigurationProfile>> GetByNodeIdAsync(
        string nodeId, CancellationToken ct = default);

    /// <summary>Returns a single setting for a node by key.</summary>
    Task<NodeConfigurationProfile?> GetByNodeAndKeyAsync(
        string nodeId, string settingKey, CancellationToken ct = default);

    /// <summary>Returns all entries for a node filtered by category.</summary>
    Task<IReadOnlyList<NodeConfigurationProfile>> GetByNodeAndCategoryAsync(
        string nodeId, string category, CancellationToken ct = default);

    /// <summary>Adds a collection of configuration entries (bulk insert).</summary>
    Task AddRangeAsync(
        IEnumerable<NodeConfigurationProfile> profiles, CancellationToken ct = default);

    /// <summary>Checks whether a node has any configuration profiles.</summary>
    Task<bool> ExistsForNodeAsync(string nodeId, CancellationToken ct = default);
}
