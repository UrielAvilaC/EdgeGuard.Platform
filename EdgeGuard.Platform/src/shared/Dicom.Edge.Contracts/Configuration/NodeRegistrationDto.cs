namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Sent by the Edge Node to the Hub during initial registration.
/// </summary>
public sealed class NodeRegistrationRequest
{
    public string NodeName    { get; set; } = default!;
    public string AeTitle     { get; set; } = default!;
    public string IpAddress   { get; set; } = default!;
    public int    DicomPort   { get; set; } = 11112;
    public string Version     { get; set; } = default!;
    public string? Location   { get; set; }
    public string? FacilityName { get; set; }
    public string? TimeZone   { get; set; }
    public string? ContactEmail { get; set; }
}

/// <summary>
/// Returned by the Hub after a successful node registration.
/// </summary>
public sealed class NodeRegistrationResponse
{
    /// <summary>ID assigned by the Hub to this node.</summary>
    public string HubNodeId   { get; set; } = default!;

    /// <summary>API key the node must use for subsequent Hub calls.</summary>
    public string ApiKey      { get; set; } = default!;

    /// <summary>Initial configuration pushed with the registration response.</summary>
    public NodeConfigurationDto? InitialConfig { get; set; }

    public DateTime RegisteredAt { get; set; }
}
