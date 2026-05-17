using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;

namespace Dicom.Edge.Hub.Api.Mapping;

public static class NodeDicomRoutingRuleMappingProfile
{
    public static NodeDicomRoutingRuleDto ToDto(this NodeDicomRoutingRule r) => new()
    {
        Id                  = r.Id,
        NodeId              = r.NodeId,
        Name                = r.Name,
        Priority            = r.Priority,
        IsEnabled           = r.IsEnabled,
        MatchModality       = r.MatchModality,
        MatchSourceAeTitle  = r.MatchSourceAeTitle,
        MatchInstitution    = r.MatchInstitution,
        MatchStudyDesc      = r.MatchStudyDesc,
        MinInstanceCount    = r.MinInstanceCount,
        MaxInstanceCount    = r.MaxInstanceCount,
        DestinationAeTitle  = r.DestinationAeTitle,
        SendToPacs          = r.SendToPacs,
        SendToHub           = r.SendToHub,
        AnonymizeBeforeSend = r.AnonymizeBeforeSend,
        MatchCount          = r.MatchCount,
        LastMatchedAt       = r.LastMatchedAt,
        CreatedAt           = r.CreatedAt,
        UpdatedAt           = r.UpdatedAt,
    };
}
