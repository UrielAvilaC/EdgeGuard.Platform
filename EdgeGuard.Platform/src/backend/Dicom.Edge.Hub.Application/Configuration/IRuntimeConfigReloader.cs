namespace Dicom.Edge.Hub.Application.Configuration;

/// <summary>
/// Re-reads every configuration provider at runtime — including the
/// <c>system_settings</c> database provider — so that <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/>
/// consumers observe new values without restarting the Hub.
/// Call after persisting a <c>system_settings</c> change.
/// </summary>
public interface IRuntimeConfigReloader
{
    /// <summary>Triggers a reload of all configuration sources.</summary>
    void Reload();
}
