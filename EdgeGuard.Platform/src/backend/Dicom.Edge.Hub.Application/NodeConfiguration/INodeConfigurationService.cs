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

    /// <summary>Returns the distinct list of categories present for a node.</summary>
    Task<IReadOnlyList<string>> GetCategoriesAsync(
        string nodeId, CancellationToken ct = default);

    /// <summary>Updates a single setting value for a node.</summary>
    Task<bool> UpdateSettingAsync(
        string nodeId, string settingKey, string newValue, CancellationToken ct = default);

    /// <summary>Updates multiple settings in a single operation. Returns counts of updated/not-found keys.</summary>
    Task<BatchUpdateNodeSettingsResponse> UpdateBatchAsync(
        string nodeId, BatchUpdateNodeSettingsRequest request, CancellationToken ct = default);

    /// <summary>Resets a setting to its shared default value.</summary>
    Task<bool> ResetSettingAsync(
        string nodeId, string settingKey, CancellationToken ct = default);

    /// <summary>Resets all settings in a category to their shared default values.</summary>
    Task<int> ResetCategoryAsync(
        string nodeId, string category, CancellationToken ct = default);

    /// <summary>
    /// Initializes default configuration profiles for a node (idempotent).
    /// <paramref name="seedOverrides"/> reemplaza el valor por defecto de las claves que
    /// indique, para sembrar de entrada lo que el nodo reportó en vez de un genérico:
    /// el caso que motivó esto es el AE title, donde el default "EDGE_NODE" se acababa
    /// empujando de vuelta al nodo y borrando el suyo.
    /// </summary>
    Task InitializeNodeDefaultsAsync(
        string nodeId,
        IReadOnlyDictionary<string, string>? seedOverrides = null,
        CancellationToken ct = default);

    /// <summary>
    /// Lee el valor de una sola clave, o null si el nodo no tiene esa fila. A diferencia
    /// de <see cref="GetNodeConfigAsync"/> NO dispara la inicialización perezosa de
    /// defaults: sirve para inspeccionar el estado real sin crearlo de paso.
    /// </summary>
    Task<string?> GetSettingValueAsync(
        string nodeId, string settingKey, CancellationToken ct = default);

    /// <summary>Computes a SHA-256 version hash from the node's current config.</summary>
    Task<string> ComputeConfigVersionAsync(string nodeId, CancellationToken ct = default);

    /// <summary>Builds a <see cref="NodeConfigSyncDto"/> ready to push to the node.</summary>
    Task<NodeConfigSyncDto> BuildSyncPayloadAsync(string nodeId, CancellationToken ct = default);
}
