using Dicom.Edge.Contracts.Node;

namespace Dicom.Edge.Abstractions.Monitoring;

/// <summary>
/// Provides access to the latest PACS C-ECHO verification results.
/// Implemented by the periodic C-ECHO monitor hosted service.
/// </summary>
public interface IPacsCEchoMonitor
{
    /// <summary>
    /// Returns the latest C-ECHO results for all configured PACS destinations.
    /// </summary>
    IReadOnlyList<PacsCEchoResultDto> GetLatestResults();
}
