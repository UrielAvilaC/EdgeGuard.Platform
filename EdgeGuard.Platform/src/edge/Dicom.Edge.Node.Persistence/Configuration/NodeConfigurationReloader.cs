namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// Holds a weak reference to the <see cref="NodeDatabaseConfigurationProvider"/>
/// so it can be triggered from application services after each DB write.
/// Registered as a singleton in DI; the provider registers itself during
/// <see cref="NodeDatabaseConfigurationSource.Build"/>.
/// </summary>
internal sealed class NodeConfigurationReloader : INodeConfigurationReloader
{
    private NodeDatabaseConfigurationProvider? _provider;

    internal void Register(NodeDatabaseConfigurationProvider provider)
        => _provider = provider;

    /// <inheritdoc/>
    public void Reload() => _provider?.TriggerReload();
}
