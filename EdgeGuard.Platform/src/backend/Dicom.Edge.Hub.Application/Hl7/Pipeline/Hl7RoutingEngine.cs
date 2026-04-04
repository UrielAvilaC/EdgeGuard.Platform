using Dicom.Edge.Hub.Application.Constants;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Hub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Hl7.Pipeline;

/// <summary>
/// Evaluates routing rules by priority (ascending). First matching rule wins.
/// Falls back to the first active node if no rules match (default routing).
/// </summary>
public sealed class Hl7RoutingEngine : IHl7RoutingEngine
{
    private readonly IHl7RoutingRuleRepository _ruleRepository;
    private readonly INodeRepository _nodeRepository;
    private readonly ILogger<Hl7RoutingEngine> _logger;

    public Hl7RoutingEngine(
        IHl7RoutingRuleRepository ruleRepository,
        INodeRepository nodeRepository,
        ILogger<Hl7RoutingEngine> logger)
    {
        _ruleRepository = ruleRepository;
        _nodeRepository = nodeRepository;
        _logger = logger;
    }

    public async Task<Hl7RouteResult> EvaluateAsync(Hl7Message message, CancellationToken ct = default)
    {
        var rules = await _ruleRepository.GetEnabledOrderedAsync(ct);

        foreach (var rule in rules)
        {
            if (!rule.Matches(message.MessageType, message.TriggerEvent, message.SendingFacility, message.SendingApplication))
                continue;

            var node = await _nodeRepository.GetByIdAsync(rule.TargetNodeId, ct);
            if (node is null || !node.IsEnabled)
            {
                _logger.LogWarning(
                    "Rule {RuleId} matched but target node {NodeId} is unavailable",
                    rule.Id, rule.TargetNodeId);
                continue;
            }

            rule.RecordMatch();
            await _ruleRepository.UpdateAsync(rule, ct);

            _logger.LogInformation(
                "Message {MessageId} routed to node {NodeName} ({NodeId}) by rule {RuleName}",
                message.Id, node.Name, node.Id, rule.Name);

            return Hl7RouteResult.Routed(node.Id, node.Name, rule.Priority, rule.Id);
        }

        // Fallback: route to first active node
        var activeNodes = await _nodeRepository.GetActiveNodesAsync(ct);
        if (activeNodes.Count > 0)
        {
            var fallback = activeNodes[0];
            _logger.LogWarning(
                "No routing rule matched for {MessageId}. Fallback to node {NodeName}",
                message.Id, fallback.Name);
            return Hl7RouteResult.Routed(
                fallback.Id, fallback.Name, Hl7ValidationConstants.FallbackPriority, Hl7ValidationConstants.FallbackRuleId);
        }

        _logger.LogError("No routing rule matched and no active nodes available for {MessageId}", message.Id);
        return Hl7RouteResult.NoRoute(Hl7ValidationConstants.NoActiveNodesReason);
    }
}
