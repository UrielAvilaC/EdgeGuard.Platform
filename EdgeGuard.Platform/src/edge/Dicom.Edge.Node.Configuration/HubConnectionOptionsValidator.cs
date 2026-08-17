using Microsoft.Extensions.Options;
namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Fails startup when Hub integration is enabled without a usable Hub address.
/// <para>
/// There is no default Hub address anywhere in the stack: appsettings (or an environment
/// variable) is the source of truth, and the <c>hub.*</c> rows in <c>node_settings</c> are
/// hydrated from it. Validating the composed value catches both a missing deployment setting
/// and a partial/corrupt row set in the database, instead of letting the node run while every
/// call to the Hub fails against an address nobody configured.
/// </para>
/// </summary>
internal sealed class HubConnectionOptionsValidator : IValidateOptions<HubConnectionOptions>
{
    public ValidateOptionsResult Validate(string? name, HubConnectionOptions options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;

        if (string.IsNullOrWhiteSpace(options.HubBaseUrl))
            return ValidateOptionsResult.Fail(
                $"{HubConnectionOptions.SectionName}:HubBaseUrl is required when " +
                $"{HubConnectionOptions.SectionName}:Enabled is true. Set it in " +
                "appsettings.<Environment>.json or via the HubConnection__HubBaseUrl " +
                "environment variable (e.g. \"http://10.10.10.32:80\") — there is no default.");

        if (!Uri.TryCreate(options.HubBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return ValidateOptionsResult.Fail(
                $"{HubConnectionOptions.SectionName}:HubBaseUrl ('{options.HubBaseUrl}') is not an " +
                "absolute http/https URL. Expected e.g. \"http://10.10.10.32:80\".");

        return ValidateOptionsResult.Success;
    }
}
