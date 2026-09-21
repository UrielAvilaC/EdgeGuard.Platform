using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Manages node configuration profiles on the Hub side.
/// Initializes defaults from <see cref="SharedNodeSettingDefaults"/> and supports
/// per-setting overrides, version computation, and sync payload generation.
/// </summary>
public sealed class NodeConfigurationService(
    INodeConfigurationProfileRepository repository,
    INodeRepository nodeRepository,
    IUnitOfWork unitOfWork,
    ILogger<NodeConfigurationService> logger) : INodeConfigurationService
{
    public async Task<IReadOnlyList<NodeConfigurationProfileDto>> GetNodeConfigAsync(
        string nodeId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(nodeId, ct);
        var profiles = await repository.GetByNodeIdAsync(nodeId, ct);
        return profiles.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<NodeConfigurationProfileDto>> GetNodeConfigByCategoryAsync(
        string nodeId, string category, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(nodeId, ct);
        var profiles = await repository.GetByNodeAndCategoryAsync(nodeId, category, ct);
        return profiles.Select(MapToDto).ToList();
    }

    public async Task<bool> UpdateSettingAsync(
        string nodeId, string settingKey, string newValue, CancellationToken ct = default)
    {
        var profile = await repository.GetByNodeAndKeyAsync(nodeId, settingKey, ct);
        if (profile is null)
        {
            logger.LogWarning(
                "Setting {Key} not found for node {NodeId}", settingKey, nodeId);
            return false;
        }

        profile.UpdateValue(newValue);
        await unitOfWork.SaveChangesAsync(ct);

        if (string.Equals(settingKey, SharedNodeSettingKeys.Dicom.AeTitle, StringComparison.Ordinal))
            await SyncCatalogAeTitleAsync(nodeId, ct);

        logger.LogInformation(
            "Node {NodeId} setting {Key} updated to {Value}",
            nodeId, settingKey, newValue);
        return true;
    }

    /// <summary>
    /// Mantiene <c>nodes.ae_title</c> igual al ajuste <c>dicom.ae_title</c>, que es la
    /// fuente única del AE del nodo.
    ///
    /// <para>La columna es una copia para que el catálogo pueda listar, buscar y ordenar
    /// por AE sin tocar los perfiles, y para que el índice único siga impidiendo dos nodos
    /// con el mismo AE. Antes nadie la actualizaba —se fijaba en el alta y ahí se quedaba—,
    /// así que al cambiar el AE el catálogo seguía mostrando el anterior. El operador veía
    /// dos valores distintos para el mismo nodo y el incorrecto era el más a mano.</para>
    ///
    /// <para>No interrumpe el guardado del ajuste: la fuente ya quedó escrita y es la que
    /// gobierna la asociación DICOM. Si el espejo no puede aplicarse se registra, porque
    /// entonces el catálogo queda desactualizado y conviene saberlo.</para>
    /// </summary>
    public async Task SyncCatalogAeTitleAsync(string nodeId, CancellationToken ct = default)
    {
        var newValue = await GetSettingValueAsync(nodeId, SharedNodeSettingKeys.Dicom.AeTitle, ct);

        if (string.IsNullOrWhiteSpace(newValue))
            return;

        AeTitle canonical;
        try
        {
            canonical = AeTitle.Create(newValue);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Node {NodeId}: '{Value}' is not a valid AE title — the catalogue column keeps its previous value",
                nodeId, newValue);
            return;
        }

        // Dos nodos con el mismo AE title colisionan en las asociaciones DICOM, y además
        // el índice único rechazaría la escritura. Se comprueba antes para poder explicar
        // cuál es el otro nodo en vez de dejar una violación de índice en el log.
        var enConflicto = (await nodeRepository.GetAllAsync(ct))
            .FirstOrDefault(n => n.Id != nodeId
                              && string.Equals(n.AeTitle.Value, canonical.Value, StringComparison.OrdinalIgnoreCase));

        if (enConflicto is not null)
        {
            logger.LogWarning(
                "Node {NodeId}: AE '{AeTitle}' already belongs to node {OtherNodeId} ({OtherName}). "
                + "The setting was saved and governs the DICOM association, but the catalogue column "
                + "was left untouched — two nodes sharing an AE will collide and must be resolved",
                nodeId, canonical.Value, enConflicto.Id, enConflicto.Name);
            return;
        }

        var node = await nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null) return;

        if (!node.SyncAeTitleFromConfiguration(canonical)) return;

        await nodeRepository.UpdateAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Node {NodeId} catalogue AE synced to {AeTitle} from dicom.ae_title",
            nodeId, canonical.Value);
    }

    public async Task<string?> GetSettingValueAsync(
        string nodeId, string settingKey, CancellationToken ct = default)
    {
        var profile = await repository.GetByNodeAndKeyAsync(nodeId, settingKey, ct);
        return profile?.Value;
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        string nodeId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(nodeId, ct);
        var profiles = await repository.GetByNodeIdAsync(nodeId, ct);
        return profiles.Select(p => p.Category)
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .OrderBy(c => c)
                       .ToList();
    }

    public async Task<BatchUpdateNodeSettingsResponse> UpdateBatchAsync(
        string nodeId, BatchUpdateNodeSettingsRequest request, CancellationToken ct = default)
    {
        var updated = 0;
        var notFound = 0;
        var failedKeys = new List<string>();

        foreach (var item in request.Settings)
        {
            try
            {
                var profile = await repository.GetByNodeAndKeyAsync(nodeId, item.Key, ct);
                if (profile is null)
                {
                    notFound++;
                    failedKeys.Add(item.Key);
                    logger.LogWarning("Batch update: setting {Key} not found for node {NodeId}", item.Key, nodeId);
                    continue;
                }

                profile.UpdateValue(item.Value);
                updated++;
            }
            catch (Exception ex)
            {
                failedKeys.Add(item.Key);
                logger.LogError(ex, "Batch update: failed to update {Key} for node {NodeId}", item.Key, nodeId);
            }
        }

        if (updated > 0)
            await unitOfWork.SaveChangesAsync(ct);

        if (request.Settings.Any(s => string.Equals(s.Key, SharedNodeSettingKeys.Dicom.AeTitle, StringComparison.Ordinal)))
            await SyncCatalogAeTitleAsync(nodeId, ct);

        logger.LogInformation(
            "Batch update for node {NodeId}: Updated={Updated}, NotFound={NotFound}",
            nodeId, updated, notFound);

        return new BatchUpdateNodeSettingsResponse
        {
            Updated = updated,
            NotFound = notFound,
            FailedKeys = failedKeys
        };
    }

    public async Task<int> ResetCategoryAsync(
        string nodeId, string category, CancellationToken ct = default)
    {
        var profiles = await repository.GetByNodeAndCategoryAsync(nodeId, category, ct);
        if (profiles.Count == 0) return 0;

        var defaultsByKey = SharedNodeSettingDefaults.All
            .ToDictionary(d => d.Key, d => d.DefaultValue, StringComparer.OrdinalIgnoreCase);

        var count = 0;
        foreach (var profile in profiles)
        {
            if (defaultsByKey.TryGetValue(profile.SettingKey, out var defaultValue))
            {
                profile.ResetToDefault(defaultValue);
                count++;
            }
        }

        if (count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        // Restablecer la categoría DICOM devuelve dicom.ae_title a su default, así que el
        // catálogo tiene que seguirlo o volvería a mostrar el AE anterior.
        if (profiles.Any(p => string.Equals(p.SettingKey, SharedNodeSettingKeys.Dicom.AeTitle, StringComparison.Ordinal)))
            await SyncCatalogAeTitleAsync(nodeId, ct);

        logger.LogInformation(
            "Reset {Count} settings in category '{Category}' for node {NodeId}",
            count, category, nodeId);

        return count;
    }

    public async Task<bool> ResetSettingAsync(
        string nodeId, string settingKey, CancellationToken ct = default)
    {
        var profile = await repository.GetByNodeAndKeyAsync(nodeId, settingKey, ct);
        if (profile is null)
        {
            logger.LogWarning(
                "Setting {Key} not found for node {NodeId}", settingKey, nodeId);
            return false;
        }

        var defaultEntry = SharedNodeSettingDefaults.All
            .FirstOrDefault(d => d.Key.Equals(settingKey, StringComparison.OrdinalIgnoreCase));

        if (defaultEntry is null)
        {
            logger.LogWarning("No shared default found for key {Key}", settingKey);
            return false;
        }

        profile.ResetToDefault(defaultEntry.DefaultValue);
        await unitOfWork.SaveChangesAsync(ct);

        if (string.Equals(settingKey, SharedNodeSettingKeys.Dicom.AeTitle, StringComparison.Ordinal))
            await SyncCatalogAeTitleAsync(nodeId, ct);

        logger.LogInformation(
            "Node {NodeId} setting {Key} reset to default {Value}",
            nodeId, settingKey, defaultEntry.DefaultValue);
        return true;
    }

    public async Task InitializeNodeDefaultsAsync(
        string nodeId,
        IReadOnlyDictionary<string, string>? seedOverrides = null,
        CancellationToken ct = default)
    {
        if (await repository.ExistsForNodeAsync(nodeId, ct))
        {
            logger.LogDebug("Node {NodeId} already has configuration profiles", nodeId);
            return;
        }

        var profiles = SharedNodeSettingDefaults.All
            .Select(d => NodeConfigurationProfile.CreateDefault(
                nodeId, d.Key, SeedValueFor(d, seedOverrides), d.Category, d.DisplayName, d.ValueType))
            .ToList();

        await repository.AddRangeAsync(profiles, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Initialized {Count} default config profiles for node {NodeId}",
            profiles.Count, nodeId);
    }

    public async Task<string> ComputeConfigVersionAsync(
        string nodeId, CancellationToken ct = default)
    {
        var profiles = await repository.GetByNodeIdAsync(nodeId, ct);
        return ComputeHash(profiles, await ReadStorageLimitAsync(nodeId, ct));
    }

    /// <summary>
    /// Lee el límite tal como se serializa en el payload de sync. Vive en el
    /// agregado Node y no en los perfiles, así que las dos rutas que calculan la
    /// versión tienen que obtenerlo por aquí para no producir hashes distintos.
    /// </summary>
    private async Task<string> ReadStorageLimitAsync(string nodeId, CancellationToken ct)
    {
        var node = await nodeRepository.GetByIdAsync(nodeId, ct);
        return (node?.StorageLimitMb ?? 0).ToString(CultureInfo.InvariantCulture);
    }

    public async Task<NodeConfigSyncDto> BuildSyncPayloadAsync(
        string nodeId, CancellationToken ct = default)
    {
        var profiles = await repository.GetByNodeIdAsync(nodeId, ct);

        // hub.api_key is never included in sync payloads — the hub only stores the hash,
        // not the raw key, so pushing an empty value would overwrite the node's stored key.
        var settings = profiles
            .Where(p => !p.SettingKey.Equals(SharedNodeSettingKeys.Hub.ApiKey, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.SettingKey, p => p.Value);

        // El límite de almacenamiento se administra en el agregado Node, no como
        // una fila de perfil, y se inyecta aquí al bajarlo. Tenerlo en los dos
        // lados sería tener dos verdades sobre el mismo número, y la del perfil
        // podría contradecir la que muestra la pantalla. Un solo escritor, un
        // solo lector, y el nodo lo devuelve en su reporte para confirmarlo.
        var storageLimitMb = await ReadStorageLimitAsync(nodeId, ct);
        settings[SharedNodeSettingKeys.Storage.LimitMb] = storageLimitMb;

        var version = ComputeHash(profiles, storageLimitMb);

        return new NodeConfigSyncDto
        {
            ConfigVersion = version,
            GeneratedAtUtc = DateTime.UtcNow,
            NodeId = nodeId,
            Settings = settings,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task EnsureInitializedAsync(string nodeId, CancellationToken ct)
    {
        if (!await repository.ExistsForNodeAsync(nodeId, ct))
            await InitializeNodeDefaultsAsync(nodeId, seedOverrides: null, ct);
    }

    /// <summary>
    /// Valor con el que nace un perfil: el override si la clave trae uno no vacío, si no
    /// el default compartido. Un override vacío se ignora a propósito — significa que el
    /// nodo todavía no tiene el dato, y sembrar vacío dejaría la pantalla en blanco.
    /// </summary>
    private static string SeedValueFor(
        NodeSettingDefaultEntry entry,
        IReadOnlyDictionary<string, string>? seedOverrides) =>
        seedOverrides is not null
        && seedOverrides.TryGetValue(entry.Key, out var seeded)
        && !string.IsNullOrWhiteSpace(seeded)
            ? seeded
            : entry.DefaultValue;

    private static string ComputeHash(
        IReadOnlyList<NodeConfigurationProfile> profiles, string storageLimitMb)
    {
        var sorted = profiles
            .OrderBy(p => p.SettingKey, StringComparer.OrdinalIgnoreCase)
            .Select(p => $"{p.SettingKey}={p.Value}");

        // El límite entra en el hash aunque no sea una fila de perfil: si no, el
        // nodo recibiría una versión idéntica tras cambiarlo y se creería al día.
        var payload = string.Join('\n', sorted.Append(
            $"{SharedNodeSettingKeys.Storage.LimitMb}={storageLimitMb}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }

    private static NodeConfigurationProfileDto MapToDto(NodeConfigurationProfile entity) => new()
    {
        NodeId = entity.NodeId,
        SettingKey = entity.SettingKey,
        Value = entity.Value,
        Category = entity.Category,
        DisplayName = entity.DisplayName,
        ValueType = entity.ValueType,
        IsOverridden = entity.IsOverridden,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
