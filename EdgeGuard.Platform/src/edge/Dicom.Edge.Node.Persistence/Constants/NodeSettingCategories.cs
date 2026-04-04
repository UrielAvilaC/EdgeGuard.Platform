using Dicom.Edge.Contracts.Configuration;

namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// Category names for grouping node settings in the database.
/// Backward-compatible aliases to <see cref="SharedNodeSettingCategories"/>.
/// </summary>
public static class NodeSettingCategories
{
    public const string General    = SharedNodeSettingCategories.General;
    public const string Hub        = SharedNodeSettingCategories.Hub;
    public const string Dicom      = SharedNodeSettingCategories.Dicom;
    public const string Cleanup    = SharedNodeSettingCategories.Cleanup;
    public const string Transfer   = SharedNodeSettingCategories.Transfer;
    public const string Security   = SharedNodeSettingCategories.Security;
    public const string Storage    = SharedNodeSettingCategories.Storage;
    public const string PacsSender = SharedNodeSettingCategories.PacsSender;
    public const string PacsCEcho  = SharedNodeSettingCategories.PacsCEcho;
    public const string NodeApi    = SharedNodeSettingCategories.NodeApi;
}
