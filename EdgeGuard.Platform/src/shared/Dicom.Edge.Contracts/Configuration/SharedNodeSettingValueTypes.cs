namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Supported value types for node settings.
/// Used to drive UI rendering and type-safe parsing.
/// </summary>
public static class SharedNodeSettingValueTypes
{
    public const string String   = "string";
    public const string Int      = "int";
    public const string Bool     = "bool";
    public const string Json     = "json";
    public const string TimeSpan = "timespan";
}
