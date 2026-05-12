using Dicom.Edge.Abstractions.Persistence;
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
    IUnitOfWork unitOfWork,
    ILogger<Hl7StudySyncService> logger) : IHl7StudySyncService
{
    // Only ORM (Order) and SIU (Scheduling) messages carry study scheduling info
    private static readonly HashSet<string> _supportedMessageTypes =
        new(StringComparer.OrdinalIgnoreCase) { "ORM", "SIU", "OMG", "OML" };

    public async Task SyncFromHl7Async(Hl7Message message, CancellationToken ct = default)
    {
        if (!_supportedMessageTypes.Contains(message.MessageType))
        {
            logger.LogInformation(
                "Study sync skipped for {MessageId}: message type {MessageType} does not carry scheduling data",
                message.Id, message.MessageType);
            return;
        }

        if (string.IsNullOrWhiteSpace(message.AccessionNumber))
        {
            logger.LogInformation(
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

        // Parse study date from OBR-7 (yyyyMMdd[HHmmss])
        DateTime? studyDate = null;
        if (!string.IsNullOrWhiteSpace(message.StudyDate) &&
            DateTime.TryParseExact(
                message.StudyDate[..Math.Min(8, message.StudyDate.Length)],
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsedDate))
        {
            studyDate = parsedDate;
        }

        var study = Study.CreateFromWorklist(
            accessionNumber:    message.AccessionNumber,
            patientId:          message.PatientId,
            patientName:        message.PatientName,
            sendingFacility:    message.SendingFacility,
            studyDate:          studyDate,
            studyDescription:   message.ProcedureDescription,
            referringPhysician: null);

        await studyRepository.AddAsync(study, ct);
        
        

        logger.LogInformation(
            "Created scheduled study {StudyId} (AccessionNumber={AccessionNumber}) from HL7 {MessageType} message {MessageId}",
            study.Id, message.AccessionNumber, message.MessageType, message.Id);
    }
}
