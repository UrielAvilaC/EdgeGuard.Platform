using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Hub.Persistence.Configuration;

/// <summary>
/// Extension methods to add Hub database-backed configuration to the pipeline.
/// </summary>
public static class HubDatabaseConfigurationExtensions
{
    /// <summary>
    /// Adds the Hub <c>system_settings</c> table as an <see cref="IConfigurationSource"/>.
    /// Settings loaded from the database override values from appsettings.json
    /// and are mapped to <c>IOptions&lt;T&gt;</c> section paths.
    /// </summary>
    public static IConfigurationBuilder AddHubDatabaseConfiguration(
        this IConfigurationBuilder builder,
        string connectionString)
    {
        builder.Add(new HubDatabaseConfigurationSource(connectionString));
        return builder;
    }
}
