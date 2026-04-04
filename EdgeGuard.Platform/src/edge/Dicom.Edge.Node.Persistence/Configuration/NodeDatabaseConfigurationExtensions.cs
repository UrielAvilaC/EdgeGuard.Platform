using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// Extension methods to add Node database-backed configuration to the pipeline.
/// </summary>
public static class NodeDatabaseConfigurationExtensions
{
    /// <summary>
    /// Adds the Node <c>node_settings</c> table as an <see cref="IConfigurationSource"/>.
    /// Settings loaded from the database override values from appsettings.json
    /// and are mapped to <c>IOptions&lt;T&gt;</c> section paths.
    /// </summary>
    public static IConfigurationBuilder AddNodeDatabaseConfiguration(
        this IConfigurationBuilder builder,
        string connectionString)
    {
        builder.Add(new NodeDatabaseConfigurationSource(connectionString));
        return builder;
    }
}
