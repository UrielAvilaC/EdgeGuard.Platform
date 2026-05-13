using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// Extension methods to add Node database-backed configuration to the pipeline.
/// </summary>
public static class NodeDatabaseConfigurationExtensions
{
    /// <summary>
    /// Adds the Node <c>node_settings</c> table as an <see cref="IConfigurationSource"/>
    /// and registers <see cref="INodeConfigurationReloader"/> in <paramref name="services"/>
    /// so application services can trigger hot-reloads after writing to the DB.
    /// </summary>
    public static IConfigurationBuilder AddNodeDatabaseConfiguration(
        this IConfigurationBuilder builder,
        string connectionString,
        IServiceCollection services)
    {
        var reloader = new NodeConfigurationReloader();
        services.AddSingleton<INodeConfigurationReloader>(reloader);
        builder.Add(new NodeDatabaseConfigurationSource(connectionString, reloader));
        return builder;
    }
}
