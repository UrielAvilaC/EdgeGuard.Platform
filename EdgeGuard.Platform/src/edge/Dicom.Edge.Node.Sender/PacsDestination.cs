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
}
