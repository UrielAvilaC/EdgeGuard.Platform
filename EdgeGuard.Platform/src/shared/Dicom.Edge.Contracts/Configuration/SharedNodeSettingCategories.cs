namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Category names for grouping node settings.
/// Shared by both Hub and Node so they agree on category identifiers.
/// </summary>
public static class SharedNodeSettingCategories
{
    public const string General    = "General";
    public const string Hub        = "Hub";
    public const string Dicom      = "DICOM";
    public const string Cleanup    = "Cleanup";
    public const string Transfer   = "Transfer";
    public const string Security   = "Security";
    public const string Storage    = "Storage";
    public const string PacsSender = "PacsSender";
    public const string PacsCEcho       = "PacsCEcho";
    public const string NodeApi         = "NodeApi";
}
