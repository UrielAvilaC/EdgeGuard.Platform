using Dicom.Edge.Contracts.Node;

namespace Dicom.Edge.Abstractions.Monitoring;

/// <summary>
/// Reports PACS C-ECHO results to the Hub so the SPA can display per-node
/// connectivity status without polling the Node directly.
/// Never throws — failures are logged as warnings.
/// </summary>
public interface IPacsEchoHubReporter
{
    /// <summary>
    /// Sends the latest C-ECHO results for this node to the Hub.
    /// Fire-and-forget safe — returns false on failure without propagating exceptions.
    /// </summary>
    Task<bool> ReportAsync(IReadOnlyList<PacsCEchoResultDto> results, CancellationToken ct = default);
}
