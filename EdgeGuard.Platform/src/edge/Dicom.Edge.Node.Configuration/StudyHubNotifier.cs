using Dicom.Edge.Abstractions.Monitoring;
using Dicom.Edge.Contracts.Hub;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
        DateTime? studyDate = null,
        string? studyDescription = null,
        int seriesCount = 0,
        DateTime? patientBirthDate = null,
        string? patientSex = null,
        CancellationToken ct = default)
    {
        var resolvedNodeId = string.IsNullOrEmpty(nodeId)
            ? hubClient.RegisteredNodeId
            : nodeId;

        if (string.IsNullOrEmpty(resolvedNodeId))
        {
            logger.LogInformation("Skipping Hub study notification — node not yet registered");
            var hubOptions = JsonSerializer.Serialize(hubClient.ConnectionOptions);
            logger.LogInformation("Hub connection options: {Options}", hubOptions);
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
                PatientBirthDate = patientBirthDate,
                PatientSex       = patientSex,
                AccessionNumber  = accessionNumber,
                InstanceCount    = instanceCount,
                TotalSizeBytes   = totalSizeBytes,
                StudyDate        = studyDate,
                StudyDescription = studyDescription,
                SeriesCount      = seriesCount,
            };

            logger.LogInformation(
                "Sending study notification to Hub for study {StudyUid} on node {NodeId} — Patient={Patient}",
                studyInstanceUid, resolvedNodeId, patientName);

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

    public async Task<bool> NotifyPacsSendStatusAsync(
        string nodeId,
        string studyInstanceUid,
        string status,
        string? targetPacsAeTitle = null,
        string? error = null,
        CancellationToken ct = default)
    {
        var resolvedNodeId = string.IsNullOrEmpty(nodeId) ? hubClient.RegisteredNodeId : nodeId;
        if (string.IsNullOrEmpty(resolvedNodeId))
        {
            logger.LogDebug("Skipping Hub PACS status notification — node not yet registered");
            return false;
        }

        try
        {
            var request = new StudyPacsStatusNotifyRequest
            {
                NodeId            = resolvedNodeId,
                StudyInstanceUid  = studyInstanceUid,
                Status            = status,
                TargetPacsAeTitle = targetPacsAeTitle,
                Error             = error,
            };

            var success = await hubClient.NotifyStudyPacsStatusAsync(request, ct);

            if (success)
                logger.LogInformation(
                    "Hub notified of PACS status {Status} for study {StudyUid}", status, studyInstanceUid);
            else
                logger.LogWarning(
                    "Hub PACS status notification failed for study {StudyUid} ({Status})", studyInstanceUid, status);

            return success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Hub PACS status notification error for study {StudyUid} ({Status})", studyInstanceUid, status);
            return false;
        }
    }

    public async Task<bool> NotifyStudyProgressAsync(
        string nodeId,
        string studyInstanceUid,
        string? accessionNumber,
        string? patientId,
        string? patientName,
        int instanceCount,
        long totalSizeBytes,
        DateTime? studyDate = null,
        string? studyDescription = null,
        int seriesCount = 0,
        DateTime? patientBirthDate = null,
        string? patientSex = null,
        CancellationToken ct = default)
    {
        var resolvedNodeId = string.IsNullOrEmpty(nodeId) ? hubClient.RegisteredNodeId : nodeId;
        if (string.IsNullOrEmpty(resolvedNodeId))
        {
            logger.LogDebug("Skipping Hub progress notification — node not yet registered");
            return false;
        }

        try
        {
            var request = new StudyProgressNotifyRequest
            {
                NodeId           = resolvedNodeId,
                StudyInstanceUid = studyInstanceUid,
                AccessionNumber  = accessionNumber,
                PatientId        = patientId,
                PatientName      = patientName,
                PatientBirthDate = patientBirthDate,
                PatientSex       = patientSex,
                InstanceCount    = instanceCount,
                TotalSizeBytes   = totalSizeBytes,
                StudyDate        = studyDate,
                StudyDescription = studyDescription,
                SeriesCount      = seriesCount,
            };

            return await hubClient.NotifyStudyProgressAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Hub progress notification error for study {StudyUid}",
                studyInstanceUid);
            return false;
        }
    }
}
