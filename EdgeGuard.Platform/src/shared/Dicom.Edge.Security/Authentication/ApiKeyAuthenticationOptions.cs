namespace Dicom.Edge.Security.Authentication;

/// <summary>
/// Options for API key authentication used by Edge Nodes (M2M).
/// </summary>
public sealed class ApiKeyAuthenticationOptions
{
    /// <summary>HTTP header name where nodes send their API key.</summary>
    public const string HeaderName = "X-Api-Key";

    /// <summary>HTTP header name for the bootstrap token (registration only).</summary>
    public const string BootstrapHeaderName = "X-Bootstrap-Token";

    /// <summary>Authentication scheme name.</summary>
    public const string Scheme = "ApiKey";

    /// <summary>
    /// Environment variable name for the bootstrap token.
    /// Used only for initial node registration.
    /// </summary>
    public const string BootstrapTokenEnvVar = "EDGE_BOOTSTRAP_TOKEN";
}
