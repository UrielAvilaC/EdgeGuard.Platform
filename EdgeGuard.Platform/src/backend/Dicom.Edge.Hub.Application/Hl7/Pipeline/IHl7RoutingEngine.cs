using Dicom.Edge.Hub.Domain.Entities;

namespace Dicom.Edge.Hub.Application.Hl7.Pipeline;

/// <summary>
/// Result of the routing engine evaluation.
/// </summary>
public sealed class Hl7RouteResult
{
    public bool IsRouted { get; init; }
    public string? TargetNodeId { get; init; }
    public string? TargetNodeName { get; init; }
    public int Priority { get; init; } = 5;
    public string? MatchedRuleId { get; init; }
    public string? Reason { get; init; }

    public static Hl7RouteResult Routed(string nodeId, string? nodeName, int priority, string ruleId) =>
        new() { IsRouted = true, TargetNodeId = nodeId, TargetNodeName = nodeName, Priority = priority, MatchedRuleId = ruleId };

    public static Hl7RouteResult NoRoute(string reason) =>
        new() { IsRouted = false, Reason = reason };
}

/// <summary>
/// Engine that evaluates routing rules to determine the target Edge Node for an HL7 message.
/// </summary>
public interface IHl7RoutingEngine
{
    Task<Hl7RouteResult> EvaluateAsync(Hl7Message message, CancellationToken ct = default);
}
