using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.Routing;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>
/// Manages DICOM routing rules scoped to a specific Edge Node.
/// These rules control which PACS destination receives a completed DICOM study,
/// and are pushed to the node automatically after every write operation.
/// </summary>
[ApiController]
[Route("api/nodes/{nodeId}/dicom-routing-rules")]
[Authorize(Policy = Policies.ViewConfiguration)]
[EnableRateLimiting("api")]
public sealed class NodeDicomRoutingRulesController(
    INodeDicomRoutingRuleService ruleService,
    INodeDicomRoutingRuleRepository ruleRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByNode(string nodeId, CancellationToken ct)
    {
        var rules = await ruleRepository.GetByNodeIdAsync(nodeId, ct);
        return Ok(rules.Select(r => r.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string nodeId, string id, CancellationToken ct)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct);
        if (rule is null || rule.NodeId != nodeId) return NotFound();
        return Ok(rule.ToDto());
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Create(
        string nodeId,
        [FromBody] CreateNodeDicomRoutingRuleRequest request,
        CancellationToken ct)
    {
        var rule = await ruleService.CreateAsync(nodeId, request, ct);
        return CreatedAtAction(nameof(GetById), new { nodeId, id = rule.Id }, rule.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Update(
        string nodeId, string id,
        [FromBody] UpdateNodeDicomRoutingRuleRequest request,
        CancellationToken ct)
    {
        var rule = await ruleService.UpdateAsync(id, request, ct);
        if (rule is null || rule.NodeId != nodeId) return NotFound();
        return Ok(rule.ToDto());
    }

    [HttpPut("{id}/enable")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Enable(string nodeId, string id, CancellationToken ct)
    {
        var found = await ruleService.EnableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/disable")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Disable(string nodeId, string id, CancellationToken ct)
    {
        var found = await ruleService.DisableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/priority")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> UpdatePriority(
        string nodeId, string id,
        [FromBody] UpdatePriorityRequest request,
        CancellationToken ct)
    {
        var found = await ruleService.UpdatePriorityAsync(id, request.Priority, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Delete(string nodeId, string id, CancellationToken ct)
    {
        var found = await ruleService.DeleteAsync(id, ct);
        return found ? NoContent() : NotFound();
    }
}
