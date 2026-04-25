using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Syncs patient demographics and contact info from HL7 PID segment.
/// Creates a new patient if not found, updates contact info if found.
/// </summary>
public sealed class Hl7PatientSyncService(
    IPatientRepository patientRepository,
    IHubAuditLogRepository auditRepository,
    ILogger<Hl7PatientSyncService> logger) : IHl7PatientSyncService
{
    public async Task SyncFromHl7Async(Hl7Message message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.PatientId) ||
            string.IsNullOrWhiteSpace(message.PatientName))
            return;

        var existing = await patientRepository.GetByPatientDicomIdAsync(message.PatientId, ct);

        if (existing is not null)
        {
            var hasContactChange = false;

            if (!string.IsNullOrWhiteSpace(message.PatientPhone) || !string.IsNullOrWhiteSpace(message.PatientEmail))
            {
                existing.UpdateContactInfo(message.PatientPhone, message.PatientEmail);
                hasContactChange = true;
            }

            existing.UpdateDemographics(
                message.PatientName,
                ParseBirthDate(message.PatientBirthDate),
                message.PatientSex);

            await patientRepository.UpdateAsync(existing, ct);

            if (hasContactChange)
            {
                await auditRepository.AddAsync(HubAuditLog.Create(
                    AuditEventType.DomainEvent,
                    "PatientContactUpdatedFromHl7",
                    entityId: existing.Id,
                    entityType: "Patient",
                    details: $"{{\"patientId\":\"{existing.Id}\",\"hasPhone\":{(!string.IsNullOrWhiteSpace(message.PatientPhone)).ToString().ToLowerInvariant()},\"hasEmail\":{(!string.IsNullOrWhiteSpace(message.PatientEmail)).ToString().ToLowerInvariant()},\"hl7MessageId\":\"{message.Id}\"}}"), ct);
            }

            logger.LogDebug("Updated patient {PatientDicomId} from HL7 message {MessageId}",
                message.PatientId, message.Id);
        }
        else
        {
            var patient = Patient.Create(
                PatientIdentifier.Create(message.PatientId),
                message.PatientName,
                ParseBirthDate(message.PatientBirthDate),
                message.PatientSex,
                phoneNumber: message.PatientPhone,
                email: message.PatientEmail,
                facilitySource: message.SendingFacility);

            await patientRepository.AddAsync(patient, ct);

            logger.LogInformation("Created patient {PatientDicomId} from HL7 message {MessageId}",
                message.PatientId, message.Id);
        }
    }

    private static DateTime? ParseBirthDate(string? hl7Date)
    {
        if (string.IsNullOrWhiteSpace(hl7Date) || hl7Date.Length < 8)
            return null;

         DateTime.TryParseExact(hl7Date[..8], "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var result);
        
            return DateTime.SpecifyKind(result, DateTimeKind.Utc);
    }
}
