namespace Dicom.Edge.Node.Sender;

/// <summary>
/// Represents a PACS destination for C-STORE SCU sends.
/// </summary>
public sealed class PacsDestination
{
    public required string Id { get; init; }
    public required string AeTitle { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }
    public bool UseTls { get; init; }

    /// <summary>
    /// P0-2: When true, the sender applies the DICOM PS3.15 Basic Confidentiality
    /// anonymization profile to every instance before transmitting to this PACS.
    /// </summary>
    public bool AnonymizeBeforeSend { get; init; }
}
