using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Hub.Domain.Aggregates.Patients.Events;

namespace Dicom.Edge.Hub.Domain.Aggregates.Patients;

/// <summary>
/// Patient aggregate root. Represents a patient registered in the Hub.
/// </summary>
public sealed class Patient : AggregateRoot<string>, ISoftDeletable
{
    public PatientIdentifier PatientDicomId { get; private set; } = default!;
    public string PatientName { get; private set; } = default!;
    public DateOnly? BirthDate { get; private set; }
    public string? Sex { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? IssuerOfPatientId { get; private set; }
    public string? OtherPatientIds { get; private set; }
    public string? FacilitySource { get; private set; }

    /// <summary>
    /// ID of the surviving patient this record was merged into (ADT^A40).
    /// Non-null means this patient is a prior/deprecated record.
    /// </summary>
    public string? MergedIntoPatientId { get; private set; }

    public bool IsMerged => MergedIntoPatientId is not null;
    public string? CreatedByNodeId { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Patient() { }

    public static Patient Create(
        PatientIdentifier patientDicomId,
        string patientName,
        DateOnly? birthDate = null,
        string? sex = null,
        string? issuerOfPatientId = null,
        string? facilitySource = null,
        string? createdByNodeId = null,
        string? phoneNumber = null,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(patientName))
            throw new ArgumentException("Patient name cannot be empty.", nameof(patientName));

        var patient = new Patient
        {
            Id = IdGenerator.NewId(),
            PatientDicomId = patientDicomId,
            PatientName = patientName.Trim(),
            BirthDate = birthDate,
            Sex = sex?.Trim(),
            IssuerOfPatientId = issuerOfPatientId?.Trim(),
            FacilitySource = facilitySource?.Trim(),
            CreatedByNodeId = createdByNodeId,
            PhoneNumber = phoneNumber?.Trim(),
            Email = email?.Trim(),
            LastUpdatedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        patient.AddDomainEvent(new PatientRegisteredEvent(
            patient.Id, patientDicomId.Value, patientName, createdByNodeId));

        return patient;
    }

    public void UpdateDemographics(
        string patientName,
        DateOnly? birthDate = null,
        string? sex = null,
        string? issuerOfPatientId = null)
    {
        if (string.IsNullOrWhiteSpace(patientName))
            throw new ArgumentException("Patient name cannot be empty.", nameof(patientName));

        PatientName = patientName.Trim();
        BirthDate = birthDate;
        Sex = sex?.Trim();
        IssuerOfPatientId = issuerOfPatientId?.Trim();
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PatientUpdatedEvent(Id, PatientDicomId.Value));
    }

    /// <summary>
    /// Fills demographics that are currently empty without overwriting existing values.
    /// Used by the DICOM ingestion path: HL7/RIS is authoritative for demographics, so a
    /// C-STORE with poorer metadata may only complete gaps, never replace known data.
    /// </summary>
    public void FillMissingDemographics(
        string? patientName = null,
        DateOnly? birthDate = null,
        string? sex = null)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(PatientName) && !string.IsNullOrWhiteSpace(patientName))
        {
            PatientName = patientName.Trim();
            changed = true;
        }

        if (BirthDate is null && birthDate is not null)
        {
            BirthDate = birthDate;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(Sex) && !string.IsNullOrWhiteSpace(sex))
        {
            Sex = sex.Trim();
            changed = true;
        }

        if (!changed) return;

        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PatientUpdatedEvent(Id, PatientDicomId.Value));
    }

    /// <summary>
    /// Updates patient contact information (phone and/or email).
    /// Only overwrites fields that are provided (non-null).
    /// </summary>
    public void UpdateContactInfo(string? phoneNumber = null, string? email = null)
    {
        var changed = false;

        if (phoneNumber is not null && phoneNumber.Trim() != PhoneNumber)
        {
            PhoneNumber = phoneNumber.Trim();
            changed = true;
        }

        if (email is not null && email.Trim() != Email)
        {
            Email = email.Trim();
            changed = true;
        }

        if (changed)
        {
            LastUpdatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Marks this patient record as merged into the surviving patient (ADT^A40).
    /// The prior patient is deactivated so lookups resolve to the surviving record.
    /// </summary>
    public void MergeInto(string survivingPatientDicomId)
    {
        if (string.IsNullOrWhiteSpace(survivingPatientDicomId))
            throw new ArgumentException("Surviving patient ID cannot be empty.", nameof(survivingPatientDicomId));

        // P0-7: Self-merge guard. A patient must never merge into itself.
        if (string.Equals(survivingPatientDicomId, PatientDicomId.Value, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Cannot merge patient {PatientDicomId.Value} into itself.");

        MergedIntoPatientId = survivingPatientDicomId;
        IsActive = false;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// P0-7: Updates the merge target for chain collapsing. Used when the SURVIVING
    /// patient is later itself merged — every patient previously merged INTO this
    /// (now-prior) patient must be re-pointed to the new surviving patient.
    /// </summary>
    public void UpdateMergeTarget(string newSurvivingPatientDicomId)
    {
        if (!IsMerged)
            throw new InvalidOperationException(
                "Patient is not merged; use MergeInto for initial merge.");
        if (string.IsNullOrWhiteSpace(newSurvivingPatientDicomId))
            throw new ArgumentException("New surviving patient ID cannot be empty.",
                nameof(newSurvivingPatientDicomId));
        if (string.Equals(newSurvivingPatientDicomId, PatientDicomId.Value, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Cannot collapse merge chain into self ({PatientDicomId.Value}).");

        MergedIntoPatientId = newSurvivingPatientDicomId;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
