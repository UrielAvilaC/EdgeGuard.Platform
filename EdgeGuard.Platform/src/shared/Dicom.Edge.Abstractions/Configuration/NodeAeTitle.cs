namespace Dicom.Edge.Abstractions.Configuration;

/// <summary>
/// Single source of truth for the node's DICOM Application Entity (AE) Title.
///
/// The canonical value lives in the <c>DicomServer:AeTitle</c> configuration key
/// (DB setting <c>dicom.ae_title</c>). The node's outbound SCU Calling AE
/// (<c>PacsSender:LocalAeTitle</c>) and the AE reported to the Hub at registration
/// (<c>HubConnection:AeTitle</c>) are DERIVED from this value — they must never be
/// configured independently. Shared by the Hub and the Edge Node to keep both sides
/// homologated.
/// </summary>
public static class NodeAeTitle
{
    /// <summary>Canonical configuration path for the node AE Title.</summary>
    public const string ConfigPath = "DicomServer:AeTitle";

    /// <summary>Default AE Title used when none is configured.</summary>
    public const string Default = "EDGE_NODE";
}
