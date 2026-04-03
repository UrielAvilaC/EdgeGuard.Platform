namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// Category names for grouping node settings in the database.
/// Stored in node_settings.category column.
/// </summary>
public static class NodeSettingCategories
{
    public const string General  = "General";
    public const string Hub      = "Hub";
    public const string Dicom    = "DICOM";
    public const string Cleanup  = "Cleanup";
    public const string Transfer = "Transfer";
    public const string Security = "Security";
    public const string Storage  = "Storage";
}
