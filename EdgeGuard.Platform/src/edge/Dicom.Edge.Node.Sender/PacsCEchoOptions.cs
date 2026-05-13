namespace Dicom.Edge.Node.Sender;

/// <summary>
/// Configuration options for the periodic PACS C-ECHO monitor.
/// </summary>
public sealed class PacsCEchoOptions
{
    public const string SectionName = "PacsCEcho";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 120;
    public PacsDestination[] Destinations { get; set; } = [];
}
