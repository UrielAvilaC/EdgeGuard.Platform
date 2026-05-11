using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Syncs a study from an HL7 ORM/SIU message using AccessionNumber as the idempotency key.
/// Creates the study with status <c>Scheduled</c> if it doesn't already exist.
/// </summary>
public sealed class Hl7StudySyncService(
    IStudyRepository studyRepository,
    ILogger<Hl7StudySyncService> logger) : IHl7StudySyncService
{
    public async Task SyncFromHl7Async(Hl7Message message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.AccessionNumber))
        {
            logger.LogDebug(
                "Study sync skipped for {MessageId}: no AccessionNumber in message",
                message.Id);
            return;
        }

        var existing = await studyRepository.GetByAccessionNumberAsync(message.AccessionNumber, ct);
        if (existing is not null)
        {
            logger.LogDebug(
                "Study with AccessionNumber={AccessionNumber} already exists ({StudyId}), skipping sync",
                message.AccessionNumber, existing.Id);
            return;
        }

        DateTime? studyDate = null;
        // HL7 date fields arrive as yyyyMMdd[HHmmss]
        if (!string.IsNullOrWhiteSpace(message.PatientBirthDate) is false) { /* not study date */ }

        var study = Study.CreateFromWorklist(
            accessionNumber:   message.AccessionNumber,
            patientId:         message.PatientId,
            patientName:       message.PatientName,
            sendingFacility:   message.SendingFacility,
            studyDate:         studyDate,
            referringPhysician: null);

        await studyRepository.AddAsync(study, ct);

        logger.LogInformation(
            "Created scheduled study {StudyId} (AccessionNumber={AccessionNumber}) from HL7 message {MessageId}",
            study.Id, message.AccessionNumber, message.Id);
    }
}
