namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// Supported value types stored in node_settings.value_type column.
/// Used to drive UI rendering and type-safe parsing in NodeSettingsService.
/// </summary>
public static class NodeSettingValueTypes
{
    public const string String   = "string";
    public const string Int      = "int";
    public const string Bool     = "bool";
    public const string Json     = "json";
    public const string TimeSpan = "timespan";
}
