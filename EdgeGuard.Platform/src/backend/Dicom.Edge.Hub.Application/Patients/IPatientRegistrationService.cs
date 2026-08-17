using Dicom.Edge.Hub.Domain.Aggregates.Patients;

namespace Dicom.Edge.Hub.Application.Patients;

/// <summary>
/// Origin of the demographics being registered. Determines precedence: HL7/RIS is
/// authoritative and overwrites, DICOM only fills gaps.
/// </summary>
public enum PatientDataSource
{
    /// <summary>PID segment of an ADT/ORM/ORU message.</summary>
    Hl7,

    /// <summary>Patient module of a C-STORE received by an Edge Node.</summary>
    Dicom
}

/// <summary>Demographics carried by an incoming study or HL7 message.</summary>
public sealed record PatientRegistrationInput
{
    /// <summary>DICOM Patient ID (0010,0020) / HL7 PID-3. Required — no id, no registration.</summary>
    public required string? PatientDicomId { get; init; }

    public string? PatientName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? Sex { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? IssuerOfPatientId { get; init; }
    public string? FacilitySource { get; init; }

    /// <summary>Node that reported the study — only recorded when creating the patient.</summary>
    public string? CreatedByNodeId { get; init; }
}

/// <summary>
/// Single entry point for registering patients in the Hub catalogue. Both the HL7 sync
/// and the Edge (DICOM) ingestion path go through here, so a walk-in study that never
/// had a worklist order still lands its patient in the catalogue.
/// </summary>
public interface IPatientRegistrationService
{
    /// <summary>
    /// Creates the patient when unknown, otherwise refreshes it according to
    /// <paramref name="source"/> precedence. Returns the live patient record (the
    /// surviving one when the match is a merged record), or <c>null</c> when the input
    /// carries no usable patient id.
    /// </summary>
    Task<Patient?> EnsurePatientAsync(
        PatientRegistrationInput input,
        PatientDataSource source,
        CancellationToken ct = default);
}
