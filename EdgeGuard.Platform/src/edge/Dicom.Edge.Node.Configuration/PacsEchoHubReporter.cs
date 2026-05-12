using Dicom.Edge.Abstractions.Monitoring;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Contracts.Node;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Implements <see cref="IPacsEchoHubReporter"/> by forwarding PACS C-ECHO results
/// to the Hub via <see cref="IHubSyncClient.ReportPacsEchoAsync"/>.
/// Never throws — failures are logged as warnings so the C-ECHO cycle is not blocked.
/// </summary>
public sealed class PacsEchoHubReporter(
    IHubSyncClient hubClient,
    ILogger<PacsEchoHubReporter> logger) : IPacsEchoHubReporter
{
    public async Task<bool> ReportAsync(
        IReadOnlyList<PacsCEchoResultDto> results,
        CancellationToken ct = default)
    {
        var nodeId = hubClient.RegisteredNodeId;
        if (string.IsNullOrEmpty(nodeId))
        {
            logger.LogDebug("Skipping PACS echo Hub report — node not yet registered");
            return false;
        }

        try
        {
            var request = new NodePacsEchoReportRequest
            {
                NodeId       = nodeId,
                ReportedAtUtc = DateTime.UtcNow,
                Results      = results.Select(r => new PacsEchoDestinationResult
                {
                    AeTitle      = r.DestinationAeTitle,
                    Host         = r.Host,
                    Port         = r.Port,
                    Success      = r.Success,
                    LatencyMs    = r.LatencyMs,
                    Error        = r.Error,
                    ErrorReason  = r.ErrorReason,
                    CheckedAtUtc = r.CheckedAtUtc,
                }).ToList().AsReadOnly(),
            };

            var success = await hubClient.ReportPacsEchoAsync(request, ct);

            if (success)
                logger.LogDebug(
                    "PACS echo status reported to Hub — {Count} destination(s)",
                    results.Count);
            else
                logger.LogWarning("PACS echo Hub report was not acknowledged");

            return success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PACS echo Hub report error — results not forwarded");
            return false;
        }
    }
}
