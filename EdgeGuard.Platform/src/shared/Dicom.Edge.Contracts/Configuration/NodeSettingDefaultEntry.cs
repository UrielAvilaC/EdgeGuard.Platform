namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Represents a single default setting entry used to seed both Hub and Node databases.
/// </summary>
public sealed record NodeSettingDefaultEntry(
    string Key,
    string DefaultValue,
    string Category,
    string DisplayName,
    string ValueType,
    bool IsReadOnly = false);
