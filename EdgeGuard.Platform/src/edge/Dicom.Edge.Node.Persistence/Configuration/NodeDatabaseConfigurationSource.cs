using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// <see cref="IConfigurationSource"/> that creates a <see cref="NodeDatabaseConfigurationProvider"/>
/// to load node settings from the Edge Node SQLite database.
/// </summary>
internal sealed class NodeDatabaseConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;
    private readonly NodeConfigurationReloader _reloader;

    public NodeDatabaseConfigurationSource(string connectionString, NodeConfigurationReloader reloader)
    {
        _connectionString = connectionString;
        _reloader         = reloader;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = new NodeDatabaseConfigurationProvider(_connectionString);
        _reloader.Register(provider);
        return provider;
    }
}
