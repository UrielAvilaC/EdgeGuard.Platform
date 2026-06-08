using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Equipment;

namespace Dicom.Edge.Hub.Api.Mapping;

public static class NodeEquipmentMappingProfile
{
    /// <summary>
    /// Maps an equipment to its DTO. <c>IsOnline</c> is computed at read time from
    /// <c>LastConnectionAt</c> and the supplied <paramref name="onlineWindow"/> — it is never
    /// persisted, so it reflects presence without any background offline-flip job.
    /// </summary>
    public static NodeEquipmentDto ToDto(this NodeEquipment e, TimeSpan onlineWindow) => new()
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
        IsOnline         = e.LastConnectionAt is not null
                           && e.LastConnectionAt.Value >= DateTime.UtcNow - onlineWindow,
        CreatedAt        = e.CreatedAt,
        UpdatedAt        = e.UpdatedAt,
    };
}
