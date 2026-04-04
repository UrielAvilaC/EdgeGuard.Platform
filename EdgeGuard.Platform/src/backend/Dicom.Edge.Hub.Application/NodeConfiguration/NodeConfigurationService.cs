using System.Security.Cryptography;
using System.Text;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Manages node configuration profiles on the Hub side.
/// Initializes defaults from <see cref="SharedNodeSettingDefaults"/> and supports
/// per-setting overrides, version computation, and sync payload generation.
/// </summary>
public sealed class NodeConfigurationService(
    INodeConfigurationProfileRepository repository,
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

        logger.LogInformation(
            "Node {NodeId} setting {Key} updated to {Value}",
            nodeId, settingKey, newValue);
        return true;
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

        logger.LogInformation(
            "Node {NodeId} setting {Key} reset to default {Value}",
            nodeId, settingKey, defaultEntry.DefaultValue);
        return true;
    }

    public async Task InitializeNodeDefaultsAsync(string nodeId, CancellationToken ct = default)
    {
        if (await repository.ExistsForNodeAsync(nodeId, ct))
        {
            logger.LogDebug("Node {NodeId} already has configuration profiles", nodeId);
            return;
        }

        var profiles = SharedNodeSettingDefaults.All
            .Select(d => NodeConfigurationProfile.CreateDefault(
                nodeId, d.Key, d.DefaultValue, d.Category, d.DisplayName, d.ValueType))
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
        return ComputeHash(profiles);
    }

    public async Task<NodeConfigSyncDto> BuildSyncPayloadAsync(
        string nodeId, CancellationToken ct = default)
    {
        var profiles = await repository.GetByNodeIdAsync(nodeId, ct);
        var settings = profiles.ToDictionary(p => p.SettingKey, p => p.Value);
        var version = ComputeHash(profiles);

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
            await InitializeNodeDefaultsAsync(nodeId, ct);
    }

    private static string ComputeHash(IReadOnlyList<NodeConfigurationProfile> profiles)
    {
        var sorted = profiles
            .OrderBy(p => p.SettingKey, StringComparer.OrdinalIgnoreCase)
            .Select(p => $"{p.SettingKey}={p.Value}");

        var payload = string.Join('\n', sorted);
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
