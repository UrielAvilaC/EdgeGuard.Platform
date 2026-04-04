using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Application service for managing node configuration profiles on the Hub.
/// Provides CRUD for per-node settings and computes config version hashes.
/// </summary>
public interface INodeConfigurationService
{
    /// <summary>Returns all configuration entries for a node. Initializes defaults if none exist.</summary>
    Task<IReadOnlyList<NodeConfigurationProfileDto>> GetNodeConfigAsync(
        string nodeId, CancellationToken ct = default);

    /// <summary>Returns configuration entries for a node filtered by category.</summary>
    Task<IReadOnlyList<NodeConfigurationProfileDto>> GetNodeConfigByCategoryAsync(
        string nodeId, string category, CancellationToken ct = default);

    /// <summary>Updates a single setting value for a node.</summary>
    Task<bool> UpdateSettingAsync(
        string nodeId, string settingKey, string newValue, CancellationToken ct = default);

    /// <summary>Resets a setting to its shared default value.</summary>
    Task<bool> ResetSettingAsync(
        string nodeId, string settingKey, CancellationToken ct = default);

    /// <summary>Initializes default configuration profiles for a node (idempotent).</summary>
    Task InitializeNodeDefaultsAsync(string nodeId, CancellationToken ct = default);

    /// <summary>Computes a SHA-256 version hash from the node's current config.</summary>
    Task<string> ComputeConfigVersionAsync(string nodeId, CancellationToken ct = default);

    /// <summary>Builds a <see cref="NodeConfigSyncDto"/> ready to push to the node.</summary>
    Task<NodeConfigSyncDto> BuildSyncPayloadAsync(string nodeId, CancellationToken ct = default);
}
