using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Equipment;

namespace Dicom.Edge.Hub.Api.Mapping;

public static class NodeEquipmentMappingProfile
{
    public static NodeEquipmentDto ToDto(this NodeEquipment e) => new()
    {
        Id               = e.Id,
        NodeId           = e.NodeId,
        AeTitle          = e.AeTitle,
        DisplayName      = e.DisplayName,
        ModalityCodes    = e.ModalityCodes,
        StationAeTitle   = e.StationAeTitle,
        StationName      = e.StationName,
        IpAddress        = e.IpAddress,
        IsEnabled        = e.IsEnabled,
        Location         = e.Location,
        Department       = e.Department,
        Manufacturer     = e.Manufacturer,
        Model            = e.Model,
        Notes            = e.Notes,
        LastConnectionAt = e.LastConnectionAt,
        IsOnline         = e.IsOnline,
        CreatedAt        = e.CreatedAt,
        UpdatedAt        = e.UpdatedAt,
    };
}
