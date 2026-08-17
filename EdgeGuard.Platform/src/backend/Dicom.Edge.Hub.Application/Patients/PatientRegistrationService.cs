using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Patients;

/// <inheritdoc cref="IPatientRegistrationService"/>
public sealed class PatientRegistrationService(
    IPatientRepository patientRepository,
    IStudyRepository studyRepository,
    ILogger<PatientRegistrationService> logger) : IPatientRegistrationService
{
    /// <summary>
    /// Placeholder ids that must never reach the catalogue. Mirrors
    /// <c>NodeConstants.DefaultPatientId</c> on the Edge Node, which substitutes it when
    /// the dataset carries no Patient ID (anonymised or malformed studies).
    /// </summary>
    private static readonly string[] PlaceholderPatientIds = ["UNKNOWN", "ANONYMOUS", "NA", "N/A"];

    /// <summary>Safety bound while walking a merge chain (A→B→C→…).</summary>
    private const int MaxMergeChainDepth = 10;

    public async Task<Patient?> EnsurePatientAsync(
        PatientRegistrationInput input,
        PatientDataSource source,
        CancellationToken ct = default)
    {
        var dicomId = input.PatientDicomId?.Trim();

        if (string.IsNullOrWhiteSpace(dicomId) || IsPlaceholder(dicomId))
        {
            logger.LogDebug(
                "Patient registration skipped — no usable patient id (value='{PatientId}', source={Source})",
                input.PatientDicomId, source);
            return null;
        }

        var existing = await patientRepository.GetByPatientDicomIdAsync(dicomId, ct);

        if (existing is not null)
        {
            var target = await ResolveSurvivingAsync(existing, ct);
            await RefreshAsync(target, input, source, ct);
            return target;
        }

        return await CreateAsync(dicomId, input, source, ct);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    private async Task<Patient?> CreateAsync(
        string dicomId, PatientRegistrationInput input, PatientDataSource source, CancellationToken ct)
    {
        // Patient.Create rejects an empty name; a DICOM study with no PatientName still
        // deserves a catalogue entry, so fall back to the id itself.
        var name = string.IsNullOrWhiteSpace(input.PatientName) ? dicomId : input.PatientName;

        var patient = Patient.Create(
            PatientIdentifier.Create(dicomId),
            name,
            input.BirthDate,
            input.Sex,
            issuerOfPatientId: input.IssuerOfPatientId,
            facilitySource:    input.FacilitySource,
            createdByNodeId:   input.CreatedByNodeId,
            phoneNumber:       input.PhoneNumber,
            email:             input.Email);

        try
        {
            await patientRepository.AddAsync(patient, ct);
        }
        catch (Exception ex)
        {
            // Concurrent notify/progress for the same study race on the check-then-insert.
            // The unique index on patient_dicom_id turns the loser into a write failure —
            // resolve it by taking whichever record won.
            var winner = await patientRepository.GetByPatientDicomIdAsync(dicomId, ct);
            if (winner is null) throw;

            logger.LogDebug(ex,
                "Concurrent registration of patient {PatientDicomId} — reusing record {PatientRecordId}",
                dicomId, winner.Id);

            return await ResolveSurvivingAsync(winner, ct);
        }

        logger.LogInformation(
            "Patient {PatientDicomId} registered from {Source} (record {PatientRecordId}, node {NodeId})",
            dicomId, source, patient.Id, input.CreatedByNodeId);

        await LinkExistingStudiesAsync(dicomId, patient.Id, ct);

        return patient;
    }

    /// <summary>
    /// Attaches the patient FK to studies that arrived before the patient was registered.
    /// A completed study never notifies again, so without this it would stay unlinked.
    /// </summary>
    private async Task LinkExistingStudiesAsync(string dicomId, string patientRecordId, CancellationToken ct)
    {
        var orphans = await studyRepository.GetUnlinkedByPatientIdAsync(dicomId, ct);
        if (orphans.Count == 0) return;

        var linked = 0;
        foreach (var study in orphans)
        {
            if (!study.LinkToPatientRecord(patientRecordId)) continue;
            await studyRepository.UpdateAsync(study, ct);
            linked++;
        }

        if (linked > 0)
            logger.LogInformation(
                "Linked {Count} pre-existing study(ies) to newly registered patient {PatientDicomId}",
                linked, dicomId);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private async Task RefreshAsync(
        Patient patient, PatientRegistrationInput input, PatientDataSource source, CancellationToken ct)
    {
        if (source == PatientDataSource.Dicom)
        {
            // DICOM never overwrites what the RIS already told us — it only fills gaps.
            patient.FillMissingDemographics(input.PatientName, input.BirthDate, input.Sex);
        }
        else if (!string.IsNullOrWhiteSpace(input.PatientName))
        {
            patient.UpdateDemographics(input.PatientName, input.BirthDate, input.Sex, input.IssuerOfPatientId);
        }

        if (input.PhoneNumber is not null || input.Email is not null)
            patient.UpdateContactInfo(input.PhoneNumber, input.Email);

        await patientRepository.UpdateAsync(patient, ct);
    }

    // ── Merge chain ───────────────────────────────────────────────────────────

    /// <summary>
    /// Walks <c>MergedIntoPatientId</c> to the surviving record so studies never attach
    /// to a deprecated patient. Falls back to the last resolvable record.
    /// </summary>
    private async Task<Patient> ResolveSurvivingAsync(Patient patient, CancellationToken ct)
    {
        var current = patient;
        var visited = new HashSet<string>(StringComparer.Ordinal) { current.Id };

        for (var depth = 0; depth < MaxMergeChainDepth && current.IsMerged; depth++)
        {
            var next = await patientRepository.GetByPatientDicomIdAsync(current.MergedIntoPatientId!, ct);
            if (next is null || !visited.Add(next.Id))
                break;

            current = next;
        }

        if (!ReferenceEquals(current, patient))
            logger.LogDebug(
                "Patient {PatientDicomId} is merged — resolved to surviving record {PatientRecordId}",
                patient.PatientDicomId.Value, current.Id);

        return current;
    }

    private static bool IsPlaceholder(string dicomId) =>
        PlaceholderPatientIds.Contains(dicomId, StringComparer.OrdinalIgnoreCase);
}
