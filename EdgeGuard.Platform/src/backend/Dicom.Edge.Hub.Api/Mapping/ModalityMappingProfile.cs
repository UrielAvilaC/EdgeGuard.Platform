using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.Modalities;

namespace Dicom.Edge.Hub.Api.Mapping;

public static class ModalityMappingProfile
{
    public static ModalityDto ToDto(this Modality m) => new()
    {
        Code        = m.Code,
        DisplayName = m.DisplayName,
        IsSupported = m.IsSupported,
        IsActive    = m.IsActive,
        SortOrder   = m.SortOrder,
    };
}
