using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Studies;

/// <summary>
/// Application service for manually re-sending a Failed study to operator-chosen PACS.
/// </summary>
public sealed class StudyResendService(
    IStudyRepository studyRepository,
    INodeRepository nodeRepository,
    IStudyRequeuePushService pushService,
    IUnitOfWork unitOfWork,
    IStudyRealtimeNotifier studyRealtimeNotifier,
    ILogger<StudyResendService> logger) : IStudyResendService
{
    public async Task<StudyResendResult> RequeueAsync(
        string studyId, IReadOnlyList<string> pacsIds, CancellationToken ct = default)
    {
        if (pacsIds.Count == 0)
            return StudyResendResult.Invalid("At least one PACS destination must be selected.");

        var study = await studyRepository.GetByIdAsync(studyId, ct);
        if (study is null) return StudyResendResult.MissingStudy();

        if (study.Status != StudyStatus.Failed)
            return StudyResendResult.Invalid($"Study is not Failed (current status: {study.Status}).");

        if (string.IsNullOrWhiteSpace(study.SourceNodeId))
            return StudyResendResult.Invalid("Study has no source node — cannot resend.");

        var node = await nodeRepository.GetWithPacsAssignmentsAsync(study.SourceNodeId, ct);
        if (node is null || string.IsNullOrWhiteSpace(node.ApiEndpoint))
            return StudyResendResult.Invalid("Source node is not reachable (missing or unregistered).");

        var activePacsIds = node.PacsAssignments
            .Where(a => a.IsActive)
            .Select(a => a.PacsId)
            .ToHashSet();

        var invalidPacsIds = pacsIds.Where(id => !activePacsIds.Contains(id)).ToList();
        if (invalidPacsIds.Count > 0)
            return StudyResendResult.Invalid(
                $"PACS not assigned to this node: {string.Join(", ", invalidPacsIds)}");

        var accepted = await pushService.PushAsync(
            node.Id, study.StudyInstanceUid.Value, pacsIds, ct);

        if (!accepted)
        {
            logger.LogWarning(
                "Study {StudyId} manual resend rejected by node {NodeId}", studyId, node.Id);
            return StudyResendResult.Invalid("Node did not accept the resend request.");
        }

        study.MarkRequeuedManually(pacsIds);
        await studyRepository.UpdateAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await studyRealtimeNotifier.StatusChangedAsync(
            new StudyStatusChange(study.Id, study.Status.ToString(), study.PatientName, node.Id, DateTime.UtcNow),
            ct);

        logger.LogInformation(
            "Study {StudyId} manually requeued to node {NodeId} for PACS [{PacsIds}]",
            studyId, node.Id, string.Join(", ", pacsIds));

        return StudyResendResult.Ok();
    }
}
