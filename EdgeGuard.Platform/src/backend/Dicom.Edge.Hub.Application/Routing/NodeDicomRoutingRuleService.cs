using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Routing;

public sealed class NodeDicomRoutingRuleService(
    INodeDicomRoutingRuleRepository ruleRepository,
    INodeDicomRoutingRulePushService pushService,
    IUnitOfWork unitOfWork,
    ILogger<NodeDicomRoutingRuleService> logger) : INodeDicomRoutingRuleService
{
    public Task<IReadOnlyList<NodeDicomRoutingRule>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default) =>
        ruleRepository.GetByNodeIdAsync(nodeId, ct);

    public async Task<NodeDicomRoutingRule> CreateAsync(
        string nodeId, CreateNodeDicomRoutingRuleRequest request, CancellationToken ct = default)
    {
        var rule = NodeDicomRoutingRule.Create(
            nodeId,
            request.Name,
            request.DestinationAeTitle,
            request.Priority,
            request.MatchModality,
            request.MatchSourceAeTitle,
            request.MatchInstitution,
            request.MatchStudyDesc,
            request.MinInstanceCount,
            request.MaxInstanceCount,
            request.SendToPacs,
            request.SendToHub,
            request.AnonymizeBeforeSend);

        await ruleRepository.AddAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "DICOM routing rule created: {RuleId} '{Name}' → {AeTitle} for node {NodeId}",
            rule.Id, rule.Name, rule.DestinationAeTitle, nodeId);

        await pushService.PushAsync(nodeId, ct);
        return rule;
    }

    public async Task<NodeDicomRoutingRule?> UpdateAsync(
        string id, UpdateNodeDicomRoutingRuleRequest request, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return null;

        rule.Update(
            request.Name,
            request.Priority,
            request.MatchModality,
            request.MatchSourceAeTitle,
            request.MatchInstitution,
            request.MatchStudyDesc,
            request.MinInstanceCount,
            request.MaxInstanceCount,
            request.SendToPacs,
            request.SendToHub,
            request.AnonymizeBeforeSend);

        await ruleRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("DICOM routing rule updated: {RuleId} for node {NodeId}", id, rule.NodeId);

        await pushService.PushAsync(rule.NodeId, ct);
        return rule;
    }

    public async Task<bool> EnableAsync(string id, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return false;

        rule.Enable();
        await ruleRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await pushService.PushAsync(rule.NodeId, ct);
        return true;
    }

    public async Task<bool> DisableAsync(string id, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return false;

        rule.Disable();
        await ruleRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await pushService.PushAsync(rule.NodeId, ct);
        return true;
    }

    public async Task<bool> UpdatePriorityAsync(string id, int priority, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return false;

        rule.UpdatePriority(priority);
        await ruleRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await pushService.PushAsync(rule.NodeId, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return false;

        var nodeId = rule.NodeId;
        await ruleRepository.DeleteAsync(id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("DICOM routing rule deleted: {RuleId} for node {NodeId}", id, nodeId);

        await pushService.PushAsync(nodeId, ct);
        return true;
    }
}
