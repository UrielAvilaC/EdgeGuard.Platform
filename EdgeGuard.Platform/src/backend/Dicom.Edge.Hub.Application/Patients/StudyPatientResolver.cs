using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;

namespace Dicom.Edge.Hub.Application.Patients;

/// <summary>
/// Resolves the <see cref="Patient"/> record behind a study.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Study.PatientId"/> is the MRN as it arrived (DICOM 0010,0020 / HL7 PID-3);
/// <see cref="Study.PatientRecordId"/> is the foreign key to <c>patients.id</c>. Passing the
/// former to <c>GetByIdAsync</c> compares an MRN against a record id and returns null for
/// practically every study — which silently emptied the patient's phone, email and every
/// patient-derived template variable in all three delivery paths.
/// </para>
/// <para>
/// The MRN lookup is kept as a fallback for studies that predate the foreign key or that
/// arrived before their patient was registered.
/// </para>
/// </remarks>
public static class StudyPatientResolver
{
    public static async Task<Patient?> ResolveAsync(
        IPatientRepository patients, Study study, CancellationToken ct = default)
    {
        if (study.PatientRecordId is { Length: > 0 } recordId)
        {
            var byRecord = await patients.GetByIdAsync(recordId, ct);
            if (byRecord is not null) return byRecord;
        }

        return study.PatientId is { Length: > 0 } mrn
            ? await patients.GetByPatientDicomIdAsync(mrn, ct)
            : null;
    }
}
