using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Configuration;

/// <summary>
/// Key-value system configuration for the Hub. Supports categories and typed values.
/// </summary>
public sealed class SystemSetting : Entity<string>
{
    public string Category { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public string ValueType { get; private set; } = "string";
    public string? Description { get; private set; }
    public bool IsReadOnly { get; private set; }
    public bool IsEncrypted { get; private set; }

    private SystemSetting() { }

    public static SystemSetting Create(
        string key,
        string value,
        string category,
        string displayName,
        string valueType = "string",
        string? description = null,
        bool isReadOnly = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Setting key cannot be empty.", nameof(key));

        return new SystemSetting
        {
            Id = key.Trim().ToLowerInvariant(),
            Value = value,
            Category = category.Trim(),
            DisplayName = displayName.Trim(),
            ValueType = valueType,
            Description = description,
            IsReadOnly = isReadOnly,
        };
    }

    public void UpdateValue(string newValue)
    {
        if (IsReadOnly)
            throw new InvalidOperationException($"Setting '{Id}' is read-only.");

        Value = newValue;
        UpdatedAt = DateTime.UtcNow;
    }
}
