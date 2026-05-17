using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;

namespace Dicom.Edge.Hub.Api.Mapping;

public static class AuditLogMappingProfile
{
    public static AuditLogDto ToDto(this HubAuditLog entity) => new()
    {
        Id = entity.Id,
        EventType = entity.EventType.ToString(),
        Action = entity.Action,
        Severity = entity.Severity.ToString(),
        UserId = entity.UserId,
        UserName = entity.UserName,
        IpAddress = entity.IpAddress,
        CorrelationId = entity.CorrelationId,
        EntityId = entity.EntityId,
        EntityType = entity.EntityType,
        IsSuccess = entity.IsSuccess,
        ErrorMessage = entity.ErrorMessage,
        Details = entity.Details,
        CreatedAt = entity.CreatedAt
    };
}
