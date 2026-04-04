using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// <see cref="IConfigurationSource"/> that creates a <see cref="NodeDatabaseConfigurationProvider"/>
/// to load node settings from the Edge Node SQLite database.
/// </summary>
internal sealed class NodeDatabaseConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;

    public NodeDatabaseConfigurationSource(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new NodeDatabaseConfigurationProvider(_connectionString);
}
