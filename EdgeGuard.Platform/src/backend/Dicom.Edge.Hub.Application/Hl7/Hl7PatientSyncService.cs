using Dicom.Edge.Hub.Application.Constants;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Application.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Syncs patient records from HL7 messages.
///
/// ADT^A01 — Admission: upsert patient demographics from PID.
/// ADT^A40 — Merge Patient: mark prior patient (MRG.1) as merged into the surviving patient (PID.3),
///            then reassign all studies of the prior patient to the surviving record.
/// ORM+MRG  — If an ORM order arrives with a MRG segment the prior patient's studies are
///            reassigned to the current patient (PID.3). No patient deactivation (ORM merge
///            only affects orders/studies, not the patient record itself).
/// </summary>
public sealed class Hl7PatientSyncService(
    IPatientRepository patientRepository,
    IStudyRepository studyRepository,
    IHubAuditLogRepository auditRepository,
    IPatientRegistrationService patientRegistration,
    ILogger<Hl7PatientSyncService> logger) : IHl7PatientSyncService
{
    public async Task SyncFromHl7Async(Hl7Message message, CancellationToken ct = default)
    {
        var baseType = message.MessageType.Split('^').FirstOrDefault() ?? message.MessageType;
        var trigger  = message.TriggerEvent ?? string.Empty;

        // ── ADT — patient admission / merge ──
        if (string.Equals(baseType, Hl7ValidationConstants.AdtMessageType, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(trigger, Hl7ValidationConstants.AdtMergePatientTrigger, StringComparison.OrdinalIgnoreCase))
                await HandleAdtMergeAsync(message, ct);
            else
                await UpsertPatientFromPidAsync(message, ct);

            return;
        }

        // ── ORM — worklist order: register the patient carried in PID so scheduled
        //    orders create/refresh the patient record, then apply any MRG reassignment.
        //    (Previously only ADT created patients, so worklist-driven RIS integrations
        //    that never send ADT left the Hub with studies but no patients.)
        if (string.Equals(baseType, Hl7ValidationConstants.OrmMessageType, StringComparison.OrdinalIgnoreCase))
        {
            await UpsertPatientFromPidAsync(message, ct);

            if (message.HasMrgSegment)
                await HandleOrmMrgPatientReassignAsync(message, ct);

            return;
        }

        // ── ORU — results: ensure the patient exists for the results-first flow. ──
        if (string.Equals(baseType, Hl7ValidationConstants.OruMessageType, StringComparison.OrdinalIgnoreCase))
        {
            await UpsertPatientFromPidAsync(message, ct);
        }
    }

    // ── PID upsert (ADT admit / ORM order / ORU result) ───────────────────────

    /// <summary>
    /// Creates or updates a patient from the message's PID demographics. Shared by ADT
    /// admissions and by ORM/ORU flows so worklist orders and results register the patient
    /// even when the facility never sends ADT messages. No-op when PID lacks id or name.
    /// </summary>
    private async Task UpsertPatientFromPidAsync(Hl7Message message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.PatientId) ||
            string.IsNullOrWhiteSpace(message.PatientName))
            return;

        var patient = await patientRegistration.EnsurePatientAsync(
            new PatientRegistrationInput
            {
                PatientDicomId = message.PatientId,
                PatientName    = message.PatientName,
                BirthDate      = ParseBirthDate(message.PatientBirthDate),
                Sex            = message.PatientSex,
                PhoneNumber    = message.PatientPhone,
                Email          = message.PatientEmail,
                FacilitySource = message.SendingFacility,
            },
            PatientDataSource.Hl7,
            ct);

        if (patient is not null)
            logger.LogDebug(
                "Patient {PatientDicomId} synced from {MessageType} {MessageId}",
                message.PatientId, message.MessageType, message.Id);
    }

    // ── ADT^A40 — Merge Patient Records ──────────────────────────────────────

    private async Task HandleAdtMergeAsync(Hl7Message message, CancellationToken ct)
    {
        // MrgPriorPatientId is guaranteed non-null by Hl7ValidationService for A40
        var priorId    = message.MrgPriorPatientId!;
        var survivingId = message.PatientId!;

        if (string.Equals(priorId, survivingId, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "ADT^A40 {MessageId}: prior patient ID equals surviving ID ({Id}), skipping",
                message.Id, survivingId);
            return;
        }

        // 1. Ensure surviving patient exists (upsert)
        var surviving = await patientRepository.GetByPatientDicomIdAsync(survivingId, ct);
        if (surviving is null)
        {
            surviving = Patient.Create(
                PatientIdentifier.Create(survivingId),
                message.PatientName ?? message.MrgPriorPatientName ?? survivingId,
                ParseBirthDate(message.PatientBirthDate),
                message.PatientSex,
                facilitySource: message.SendingFacility);

            await patientRepository.AddAsync(surviving, ct);

            logger.LogInformation(
                "ADT^A40 {MessageId}: created surviving patient {SurvivingId}",
                message.Id, survivingId);
        }
        else if (!string.IsNullOrWhiteSpace(message.PatientName))
        {
            surviving.UpdateDemographics(
                message.PatientName,
                ParseBirthDate(message.PatientBirthDate),
                message.PatientSex);
            await patientRepository.UpdateAsync(surviving, ct);
        }

        // 2. Find and deactivate prior patient
        var prior = await patientRepository.GetByPatientDicomIdAsync(priorId, ct);
        if (prior is not null && !prior.IsMerged)
        {
            prior.MergeInto(survivingId);
            await patientRepository.UpdateAsync(prior, ct);

            logger.LogInformation(
                "ADT^A40 {MessageId}: patient {PriorId} merged into {SurvivingId}",
                message.Id, priorId, survivingId);

            // P0-7: Collapse merge chains. Any patient previously merged INTO this
            // prior (now itself merged) must be re-pointed to the new surviving record.
            // Without this, A→B→C would leave A pointing to B (broken chain).
            var chained = await patientRepository.GetByMergedIntoPatientIdAsync(priorId, ct);
            var collapsed = 0;
            foreach (var c in chained)
            {
                if (c.Id == prior.Id) continue;                          // self-safety
                if (c.MergedIntoPatientId == survivingId) continue;      // already correct
                if (string.Equals(c.PatientDicomId.Value, survivingId,
                        StringComparison.Ordinal)) continue;             // circular safety

                c.UpdateMergeTarget(survivingId);
                await patientRepository.UpdateAsync(c, ct);
                collapsed++;
            }

            if (collapsed > 0)
                logger.LogInformation(
                    "ADT^A40 {MessageId}: collapsed {Count} merge-chain entries from {PriorId} → {SurvivingId}",
                    message.Id, collapsed, priorId, survivingId);
        }
        else if (prior is null)
        {
            logger.LogWarning(
                "ADT^A40 {MessageId}: prior patient {PriorId} not found in Hub — cannot deactivate",
                message.Id, priorId);
        }

        // 3. Reassign all studies that belong to the prior patient — both those linked by
        //    FK and any that only carry the prior MRN.
        var priorStudies = prior is not null
            ? await studyRepository.GetByPatientAsync(prior.Id, priorId, ct)
            : await studyRepository.GetByPatientIdAsync(priorId, ct);
        if (priorStudies.Count > 0)
        {
            foreach (var study in priorStudies)
            {
                // The FK moves with the MRN — otherwise the study would still be listed
                // under the deprecated patient record.
                study.ReassignToPatient(survivingId, message.PatientName, surviving.Id);
                await studyRepository.UpdateAsync(study, ct);
            }

            logger.LogInformation(
                "ADT^A40 {MessageId}: reassigned {Count} studies from {PriorId} to {SurvivingId}",
                message.Id, priorStudies.Count, priorId, survivingId);
        }

        // 4. Audit
        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.DomainEvent,
            "PatientMergedFromHl7",
            entityId:   surviving.Id,
            entityType: "Patient",
            details:    $"{{\"survivingId\":\"{survivingId}\",\"priorId\":\"{priorId}\"," +
                        $"\"studiesReassigned\":{priorStudies.Count},\"hl7MessageId\":\"{message.Id}\"}}"), ct);
    }

    // ── ORM + MRG — Order-level patient reassignment ──────────────────────────

    private async Task HandleOrmMrgPatientReassignAsync(Hl7Message message, CancellationToken ct)
    {
        var priorId     = message.MrgPriorPatientId;
        var survivingId = message.PatientId;

        if (string.IsNullOrWhiteSpace(priorId) || string.IsNullOrWhiteSpace(survivingId))
            return;

        if (string.Equals(priorId, survivingId, StringComparison.OrdinalIgnoreCase))
            return;

        // Reassign studies belonging to the prior patient ID
        var priorStudies = await studyRepository.GetByPatientIdAsync(priorId, ct);

        // If prior accession is set, restrict to that study
        if (!string.IsNullOrWhiteSpace(message.MrgPriorAccessionNumber))
        {
            priorStudies = priorStudies
                .Where(s => string.Equals(
                    s.AccessionNumber, message.MrgPriorAccessionNumber,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (priorStudies.Count == 0)
        {
            logger.LogDebug(
                "ORM+MRG {MessageId}: no studies found for prior patient {PriorId} to reassign",
                message.Id, priorId);
            return;
        }

        var survivingPatient = await patientRepository.GetByPatientDicomIdAsync(survivingId, ct);
        var survivingName    = survivingPatient?.PatientName ?? message.PatientName;

        foreach (var study in priorStudies)
        {
            study.ReassignToPatient(survivingId, survivingName, survivingPatient?.Id);
            await studyRepository.UpdateAsync(study, ct);
        }

        logger.LogInformation(
            "ORM+MRG {MessageId}: reassigned {Count} studies from {PriorId} to {SurvivingId}",
            message.Id, priorStudies.Count, priorId, survivingId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DateOnly? ParseBirthDate(string? hl7Date)
    {
        if (string.IsNullOrWhiteSpace(hl7Date) || hl7Date.Length < 8)
            return null;

        DateOnly.TryParseExact(
            hl7Date[..8], "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var result);

        return result == default ? null : result;
    }
}
