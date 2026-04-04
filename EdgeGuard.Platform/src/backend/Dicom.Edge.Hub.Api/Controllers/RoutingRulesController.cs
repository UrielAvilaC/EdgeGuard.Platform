using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutingRulesController : ControllerBase
{
    private readonly IHl7RoutingRuleRepository _ruleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RoutingRulesController(IHl7RoutingRuleRepository ruleRepository, IUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var rules = await _ruleRepository.GetAllAsync(ct);
        return Ok(rules.Select(MapToDto));
    }

    [HttpGet("enabled")]
    public async Task<IActionResult> GetEnabled(CancellationToken ct)
    {
        var rules = await _ruleRepository.GetEnabledOrderedAsync(ct);
        return Ok(rules.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, ct);
        return rule is null ? NotFound() : Ok(MapToDto(rule));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoutingRuleRequest request, CancellationToken ct)
    {
        var rule = Hl7RoutingRule.Create(
            request.Name,
            request.TargetNodeId,
            request.Priority,
            request.MatchMessageType,
            request.MatchTriggerEvent,
            request.MatchSendingFacility,
            request.MatchSendingApplication);

        await _ruleRepository.AddAsync(rule, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, MapToDto(rule));
    }

    [HttpPut("{id}/enable")]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return NotFound();

        rule.Enable();
        await _ruleRepository.UpdateAsync(rule, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPut("{id}/disable")]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return NotFound();

        rule.Disable();
        await _ruleRepository.UpdateAsync(rule, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPut("{id}/priority")]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdatePriorityRequest request, CancellationToken ct)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, ct);
        if (rule is null) return NotFound();

        rule.UpdatePriority(request.Priority);
        await _ruleRepository.UpdateAsync(rule, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _ruleRepository.DeleteAsync(id, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    private static object MapToDto(Hl7RoutingRule r) => new
    {
        r.Id,
        r.Name,
        r.Priority,
        r.IsEnabled,
        r.MatchMessageType,
        r.MatchTriggerEvent,
        r.MatchSendingFacility,
        r.MatchSendingApplication,
        r.TargetNodeId,
        r.MatchCount,
        r.LastMatchedAt,
        r.CreatedAt,
        r.UpdatedAt
    };
}

public sealed class CreateRoutingRuleRequest
{
    public required string Name { get; init; }
    public required string TargetNodeId { get; init; }
    public int Priority { get; init; } = 100;
    public string? MatchMessageType { get; init; }
    public string? MatchTriggerEvent { get; init; }
    public string? MatchSendingFacility { get; init; }
    public string? MatchSendingApplication { get; init; }
}

public sealed class UpdatePriorityRequest
{
    public required int Priority { get; init; }
}
