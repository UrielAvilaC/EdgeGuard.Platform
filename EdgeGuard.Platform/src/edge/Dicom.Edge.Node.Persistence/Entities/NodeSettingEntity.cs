namespace Dicom.Edge.Node.Persistence.Entities;

/// <summary>
/// Represents a single row in the <c>node_settings</c> table.
/// Each row is a typed key/value pair with metadata for UI display and validation.
/// </summary>
public sealed class NodeSettingEntity : ITimestampedEntity
{
    /// <summary>Unique setting key (e.g., "hub.hostname"). Primary key.</summary>
    public string Key { get; set; } = default!;

    /// <summary>Raw string value. Parsed by NodeSettingsService according to ValueType.</summary>
    public string Value { get; set; } = default!;

    /// <summary>Category for grouping (e.g., "Hub", "DICOM"). See <see cref="NodeSettingCategories"/>.</summary>
    public string Category { get; set; } = default!;

    /// <summary>Human-readable name for UI display.</summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>Optional description shown as help text in administration UI.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parsing hint for the UI and service. See <see cref="NodeSettingValueTypes"/>.
    /// Valid values: string, int, bool, json, timespan.
    /// </summary>
    public string ValueType { get; set; } = default!;

    /// <summary>
    /// When true, SetAsync() and ApplyBatchAsync() will silently reject updates.
    /// Used for system-managed values like "node.version".
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime UpdatedAt { get; set; }
}
