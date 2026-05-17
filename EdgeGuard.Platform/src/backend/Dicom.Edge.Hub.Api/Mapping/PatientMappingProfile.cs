using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;

namespace Dicom.Edge.Hub.Api.Mapping;

/// <summary>
/// Mapping profile for <see cref="Patient"/> ↔ <see cref="PatientDto"/>.
/// </summary>
public static class PatientMappingProfile
{
    public static PatientDto ToDto(this Patient entity) => new()
    {
        Id = entity.Id,
        PatientDicomId = entity.PatientDicomId.Value,
        PatientName = entity.PatientName,
        BirthDate = entity.BirthDate,
        Sex = entity.Sex,
        PhoneNumber = entity.PhoneNumber,
        Email = entity.Email,
        IssuerOfPatientId = entity.IssuerOfPatientId,
        FacilitySource = entity.FacilitySource,
        CreatedByNodeId = entity.CreatedByNodeId,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
