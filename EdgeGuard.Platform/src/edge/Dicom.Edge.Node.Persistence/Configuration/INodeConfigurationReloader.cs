namespace Dicom.Edge.Node.Persistence.Configuration;

/// <summary>
/// Triggers a hot-reload of the <see cref="NodeDatabaseConfigurationProvider"/>.
/// Inject this into services that write to <c>node_settings</c> so that
/// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/> change
/// notifications fire immediately after a write, without restarting the host.
/// </summary>
public interface INodeConfigurationReloader
{
    /// <summary>
    /// Re-reads all settings from SQLite and notifies <c>IOptionsMonitor</c>
    /// subscribers of the change.
    /// </summary>
    void Reload();
}
