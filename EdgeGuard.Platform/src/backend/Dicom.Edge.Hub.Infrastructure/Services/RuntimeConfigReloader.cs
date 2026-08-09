using Dicom.Edge.Hub.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Default <see cref="IRuntimeConfigReloader"/> that reloads the application's
/// <see cref="IConfigurationRoot"/>. This re-runs every provider's <c>Load()</c>,
/// including <c>HubDatabaseConfigurationProvider</c>, so freshly-saved
/// <c>system_settings</c> values flow into <c>IOptionsMonitor&lt;T&gt;</c>.
/// </summary>
public sealed class RuntimeConfigReloader(
    IConfiguration configuration,
    ILogger<RuntimeConfigReloader> logger) : IRuntimeConfigReloader
{
    public void Reload()
    {
        if (configuration is IConfigurationRoot root)
        {
            root.Reload();
            logger.LogInformation("Runtime configuration reloaded from all providers (incl. system_settings)");
        }
        else
        {
            logger.LogWarning(
                "IConfiguration is not an IConfigurationRoot; runtime reload skipped (settings change applies on next restart)");
        }
    }
}
