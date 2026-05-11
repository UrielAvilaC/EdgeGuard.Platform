using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.ValueObjects;

namespace Dicom.Edge.Hub.Api.Mapping;

/// <summary>
/// Mapping profile for <see cref="Node"/> ↔ <see cref="NodeDto"/>
/// and <see cref="NodePacsAssignment"/> ↔ <see cref="NodePacsAssignmentDto"/>.
/// </summary>
public static class NodeMappingProfile
{
    public static NodeDto ToDto(this Node entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        AeTitle = entity.AeTitle.Value,
        IpAddress = entity.IpAddress,
        Port = entity.Port,
        ApiEndpoint = entity.ApiEndpoint,
        Location = entity.Location,
        FacilityName = entity.FacilityName,
        Status = entity.Status.ToString(),
        IsEnabled = entity.IsEnabled,
        LastHeartbeatAt = entity.LastHeartbeatAt,
        HealthCheckIntervalSeconds = entity.HealthCheckIntervalSeconds,
        MaxStorageMb = entity.MaxStorageMb,
        AvailableStorageMb = entity.AvailableStorageMb,
        TotalStudiesReceived = entity.TotalStudiesReceived,
        TotalStudiesSent = entity.TotalStudiesSent,
        ErrorsLast24Hours = entity.ErrorsLast24Hours,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        PacsAssignments = entity.PacsAssignments.Select(a => a.ToDto()).ToList()
    };

    public static NodePacsAssignmentDto ToDto(this NodePacsAssignment entity) => new()
    {
        PacsId = entity.PacsId,
        IsActive = entity.IsActive,
        InheritedFromHub = entity.InheritedFromHub
    };

    public static Node ToEntity(this CreateNodeRequest dto) =>
        Node.Create(
            dto.Name,
            AeTitle.Create(dto.AeTitle),
            dto.IpAddress,
            dto.Port,
            dto.ApiEndpoint,
            dto.Location,
            dto.FacilityName,
            dto.HealthCheckIntervalSeconds);
}
