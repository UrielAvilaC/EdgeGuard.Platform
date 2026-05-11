using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Routing;

/// <summary>
/// Application service for Hl7RoutingRule write operations.
/// </summary>
public sealed class RoutingRuleService(
    IHl7RoutingRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    ILogger<RoutingRuleService> logger) : IRoutingRuleService
{
    public async Task<Hl7RoutingRule> CreateAsync(CreateRoutingRuleRequest request, CancellationToken ct = default)
    {
        var rule = Hl7RoutingRule.Create(
            request.Name,
            request.TargetNodeId,
            request.Priority,
            request.MatchMessageType,
            request.MatchTriggerEvent,
            request.MatchSendingFacility,
            request.MatchSendingApplication);

        await ruleRepository.AddAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Routing rule created: {RuleId} {Name} → Node {TargetNodeId}",
            rule.Id, request.Name, request.TargetNodeId);
        return rule;
    }

    public async Task<bool> EnableAsync(string id, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return false;

        rule.Enable();
        await ruleRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Routing rule enabled: {RuleId}", id);
        return true;
    }

    public async Task<Hl7RoutingRule?> UpdateAsync(string id, UpdateRoutingRuleRequest request, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return null;

        rule.Update(
            request.Name,
            request.TargetNodeId,
            request.Priority,
            request.MatchMessageType,
            request.MatchTriggerEvent,
            request.MatchSendingFacility,
            request.MatchSendingApplication);

        await ruleRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Routing rule updated: {RuleId} {Name}", id, request.Name);
        return rule;
    }

    public async Task<bool> DisableAsync(string id, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return false;

        rule.Disable();
        await ruleRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Routing rule disabled: {RuleId}", id);
        return true;
    }

    public async Task<bool> UpdatePriorityAsync(string id, int priority, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return false;

        rule.UpdatePriority(priority);
        await ruleRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Routing rule {RuleId} priority updated to {Priority}", id, priority);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return false;

        await ruleRepository.DeleteAsync(id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Routing rule deleted: {RuleId}", id);
        return true;
    }
}
