using Dicom.Edge.Abstractions.Monitoring;
using Dicom.Edge.Contracts.Hub;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Implements <see cref="IStudyHubNotifier"/> by forwarding study metadata to the Hub
/// via <see cref="IHubSyncClient.NotifyStudyAsync"/>.
/// Never throws — failures are logged as warnings so the pipeline is not blocked.
/// </summary>
public sealed class StudyHubNotifier(
    IHubSyncClient hubClient,
    ILogger<StudyHubNotifier> logger) : IStudyHubNotifier
{
    public async Task<bool> NotifyStudyCompletedAsync(
        string nodeId,
        string studyInstanceUid,
        string? patientId,
        string? patientName,
        string? accessionNumber,
        int instanceCount,
        long totalSizeBytes,
        CancellationToken ct = default)
    {
        var resolvedNodeId = string.IsNullOrEmpty(nodeId)
            ? hubClient.RegisteredNodeId
            : nodeId;

        if (string.IsNullOrEmpty(resolvedNodeId))
        {
            logger.LogDebug("Skipping Hub study notification — node not yet registered");
            return false;
        }
        logger.LogInformation(
            "Notifying Hub of completed study {StudyUid} on node {NodeId} — Patient={Patient}",
            studyInstanceUid, resolvedNodeId, patientName);

        try
        {
            var request = new StudyNotifyRequest
            {
                NodeId           = resolvedNodeId,
                StudyInstanceUid = studyInstanceUid,
                PatientId        = patientId,
                PatientName      = patientName,
                AccessionNumber  = accessionNumber,
                InstanceCount    = instanceCount,
                TotalSizeBytes   = totalSizeBytes,
            };

            var success = await hubClient.NotifyStudyAsync(request, ct);

            if (success)
                logger.LogInformation(
                    "Hub notified of completed study {StudyUid} — Patient={Patient}",
                    studyInstanceUid, patientName);
            else
                logger.LogWarning(
                    "Hub notification failed for study {StudyUid}", studyInstanceUid);

            return success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Hub notification error for study {StudyUid} — study was still processed normally",
                studyInstanceUid);
            return false;
        }
    }
}
