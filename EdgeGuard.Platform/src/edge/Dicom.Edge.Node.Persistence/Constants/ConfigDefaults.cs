namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// Fallback / default values used by <c>NodeDatabaseConfigurationProvider</c>
/// when a database setting is absent or empty.
/// </summary>
public static class ConfigDefaults
{
    public const string HubPort        = "443";
    public const string EmptyJsonArray = "[]";
}
