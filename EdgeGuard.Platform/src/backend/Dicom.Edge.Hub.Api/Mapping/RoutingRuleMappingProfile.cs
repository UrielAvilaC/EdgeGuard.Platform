using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;

namespace Dicom.Edge.Hub.Api.Mapping;

/// <summary>
/// Mapping profile for <see cref="Hl7RoutingRule"/> ↔ <see cref="Hl7RoutingRuleDto"/>.
/// </summary>
public static class RoutingRuleMappingProfile
{
    public static Hl7RoutingRuleDto ToDto(this Hl7RoutingRule entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Priority = entity.Priority,
        IsEnabled = entity.IsEnabled,
        MatchMessageType = entity.MatchMessageType,
        MatchTriggerEvent = entity.MatchTriggerEvent,
        MatchSendingFacility = entity.MatchSendingFacility,
        MatchSendingApplication = entity.MatchSendingApplication,
        TargetNodeId = entity.TargetNodeId,
        MatchCount = entity.MatchCount,
        LastMatchedAt = entity.LastMatchedAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static Hl7RoutingRule ToEntity(this CreateRoutingRuleRequest dto) =>
        Hl7RoutingRule.Create(
            dto.Name,
            dto.TargetNodeId,
            dto.Priority,
            dto.MatchMessageType,
            dto.MatchTriggerEvent,
            dto.MatchSendingFacility,
            dto.MatchSendingApplication);
}
