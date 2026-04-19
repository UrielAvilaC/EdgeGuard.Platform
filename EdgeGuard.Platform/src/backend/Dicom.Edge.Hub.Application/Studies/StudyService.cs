using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Studies;

/// <summary>
/// Application service for Study write operations.
/// Delegates state changes to the domain aggregate and manages persistence via UoW.
/// </summary>
public sealed class StudyService(
    IStudyRepository studyRepository,
    IUnitOfWork unitOfWork,
    ILogger<StudyService> logger) : IStudyService
{
    public async Task<Study?> UpdateAsync(string id, UpdateStudyRequest request, CancellationToken ct = default)
    {
        var study = await studyRepository.GetByIdAsync(id, ct);
        if (study is null) return null;

        study.UpdateMetadata(
            studyDescription: request.StudyDescription,
            referringPhysician: request.ReferringPhysician,
            accessionNumber: request.AccessionNumber,
            priority: request.Priority,
            isUrgent: request.IsUrgent);

        await studyRepository.UpdateAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Study {StudyId} metadata updated", id);
        return study;
    }

    public async Task<(Study? Study, string? Error)> UpdateStatusAsync(
        string id, UpdateStudyStatusRequest request, CancellationToken ct = default)
    {
        var study = await studyRepository.GetByIdAsync(id, ct);
        if (study is null) return (null, null);

        if (!Enum.TryParse<StudyStatus>(request.Status, true, out var newStatus))
            return (null, $"Invalid status: {request.Status}");

        switch (newStatus)
        {
            case StudyStatus.Completed: study.MarkCompleted(); break;
            case StudyStatus.Failed: study.MarkFailed(request.Reason ?? "Manual status change"); break;
            case StudyStatus.SentToPacs: study.MarkSentToPacs(); break;
            default:
                return (null, $"Manual transition to {newStatus} is not supported");
        }

        await studyRepository.UpdateAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Study {StudyId} status manually changed to {Status}", id, newStatus);
        return (study, null);
    }
}
