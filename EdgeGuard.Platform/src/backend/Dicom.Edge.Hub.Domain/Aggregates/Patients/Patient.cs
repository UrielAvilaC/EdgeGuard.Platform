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
