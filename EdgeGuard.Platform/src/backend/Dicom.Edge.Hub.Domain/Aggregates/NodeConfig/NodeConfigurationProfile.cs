namespace Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;

/// <summary>
/// Represents the Hub's desired configuration for a specific setting on a specific node.
/// Composite PK: (NodeId, SettingKey). Does not inherit <c>Entity&lt;T&gt;</c>
/// because the identity is a composite key rather than a single ID.
/// </summary>
public sealed class NodeConfigurationProfile
{
    public string NodeId { get; private set; } = default!;
    public string SettingKey { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string ValueType { get; private set; } = "string";
    public bool IsOverridden { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private NodeConfigurationProfile() { }

    /// <summary>
    /// Creates a new configuration profile entry with default value (not overridden).
    /// </summary>
    public static NodeConfigurationProfile CreateDefault(
        string nodeId,
        string settingKey,
        string defaultValue,
        string category,
        string displayName,
        string valueType)
    {
        var now = DateTime.UtcNow;
        return new NodeConfigurationProfile
        {
            NodeId = nodeId,
            SettingKey = settingKey,
            Value = defaultValue,
            Category = category,
            DisplayName = displayName,
            ValueType = valueType,
            IsOverridden = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Updates the setting value and marks it as overridden by an admin.
    /// </summary>
    public void UpdateValue(string newValue)
    {
        Value = newValue;
        IsOverridden = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets the setting to its default value and clears the override flag.
    /// </summary>
    public void ResetToDefault(string defaultValue)
    {
        Value = defaultValue;
        IsOverridden = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
