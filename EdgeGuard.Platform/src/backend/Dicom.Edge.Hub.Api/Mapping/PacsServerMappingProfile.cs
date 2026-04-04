using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.ValueObjects;

namespace Dicom.Edge.Hub.Api.Mapping;

/// <summary>
/// Mapping profile for <see cref="PacsServer"/> ↔ <see cref="PacsServerDto"/>.
/// </summary>
public static class PacsServerMappingProfile
{
    public static PacsServerDto ToDto(this PacsServer entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        AeTitle = entity.AeTitle.Value,
        HostName = entity.HostName,
        Port = entity.Port,
        Description = entity.Description,
        IsEnabled = entity.IsEnabled,
        IsGlobal = entity.IsGlobal,
        MaxConcurrentAssociations = entity.MaxConcurrentAssociations,
        TimeoutSeconds = entity.TimeoutSeconds,
        LastCEchoAt = entity.LastCEchoAt,
        LastCEchoSuccess = entity.LastCEchoSuccess,
        IsReachable = entity.IsReachable,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static PacsServer ToEntity(this CreatePacsServerRequest dto) =>
        PacsServer.Create(
            dto.Name,
            AeTitle.Create(dto.AeTitle),
            dto.HostName,
            dto.Port,
            dto.Description,
            dto.IsGlobal,
            dto.MaxConcurrentAssociations,
            dto.TimeoutSeconds);
}
