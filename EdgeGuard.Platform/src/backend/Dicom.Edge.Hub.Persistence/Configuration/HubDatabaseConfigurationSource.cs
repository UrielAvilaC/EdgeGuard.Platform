using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Hub.Persistence.Configuration;

/// <summary>
/// <see cref="IConfigurationSource"/> that creates a <see cref="HubDatabaseConfigurationProvider"/>
/// to load system settings from the Hub PostgreSQL database.
/// </summary>
internal sealed class HubDatabaseConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;

    public HubDatabaseConfigurationSource(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new HubDatabaseConfigurationProvider(_connectionString);
}
