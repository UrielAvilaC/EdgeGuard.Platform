using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;

namespace Dicom.Edge.Hub.Api.Mapping;

/// <summary>
/// Mapping profile for <see cref="NodeConfigurationProfile"/> ↔ <see cref="NodeConfigurationProfileDto"/>.
/// </summary>
public static class NodeConfigMappingProfile
{
    public static NodeConfigurationProfileDto ToDto(this NodeConfigurationProfile entity) => new()
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
