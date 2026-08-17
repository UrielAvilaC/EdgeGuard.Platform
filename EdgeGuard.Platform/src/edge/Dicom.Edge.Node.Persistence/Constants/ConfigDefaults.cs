namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// Fallback / default values used by <c>NodeDatabaseConfigurationProvider</c>
/// when a database setting is absent or empty.
/// </summary>
public static class ConfigDefaults
{
    // No default Hub address here on purpose: appsettings (HubConnection:HubBaseUrl) is the
    // single source of truth, and a missing/incomplete DB address must fall back to it rather
    // than to an invented protocol/port. See NodeDatabaseConfigurationProvider.MapHubConnection.
    public const string EmptyJsonArray = "[]";
}
