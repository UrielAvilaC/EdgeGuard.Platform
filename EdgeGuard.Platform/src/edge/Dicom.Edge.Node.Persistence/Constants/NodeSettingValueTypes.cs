using Dicom.Edge.Contracts.Configuration;

namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// Supported value types stored in node_settings.value_type column.
/// Backward-compatible aliases to <see cref="SharedNodeSettingValueTypes"/>.
/// </summary>
public static class NodeSettingValueTypes
{
    public const string String   = SharedNodeSettingValueTypes.String;
    public const string Int      = SharedNodeSettingValueTypes.Int;
    public const string Bool     = SharedNodeSettingValueTypes.Bool;
    public const string Json     = SharedNodeSettingValueTypes.Json;
    public const string TimeSpan = SharedNodeSettingValueTypes.TimeSpan;
}
