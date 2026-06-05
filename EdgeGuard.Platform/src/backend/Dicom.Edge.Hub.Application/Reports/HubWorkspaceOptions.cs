namespace Dicom.Edge.Hub.Application.Reports;

/// <summary>
/// Configuration for the Hub workspace where report artifacts (PDFs) are stored.
/// Bound from the <c>Hub:Workspace</c> configuration section. Lives in Application so
/// both the ORU handler (size validation) and the storage implementation can use it.
/// </summary>
public sealed class HubWorkspaceOptions
{
    public const string SectionName = "Hub:Workspace";

    /// <summary>Directory where report PDFs are written (absolute or relative to the content root).</summary>
    public string ReportsPath { get; set; } = "workspace/reports";

    /// <summary>Maximum accepted report PDF size, in megabytes.</summary>
    public int MaxPdfMb { get; set; } = 25;
}
