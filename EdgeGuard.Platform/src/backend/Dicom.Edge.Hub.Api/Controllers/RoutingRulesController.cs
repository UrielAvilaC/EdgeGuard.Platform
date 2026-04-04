using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.Routing;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutingRulesController : ControllerBase
{
    private readonly IHl7RoutingRuleRepository _ruleRepository;
    private readonly IRoutingRuleService _ruleService;
    private readonly ILogger<RoutingRulesController> _logger;

    public RoutingRulesController(
        IHl7RoutingRuleRepository ruleRepository,
        IRoutingRuleService ruleService,
        ILogger<RoutingRulesController> logger)
    {
        _ruleRepository = ruleRepository;
        _ruleService = ruleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var rules = await _ruleRepository.GetAllAsync(ct);
        return Ok(rules.Select(r => r.ToDto()));
    }

    [HttpGet("enabled")]
    public async Task<IActionResult> GetEnabled(CancellationToken ct)
    {
        var rules = await _ruleRepository.GetEnabledOrderedAsync(ct);
        return Ok(rules.Select(r => r.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, ct);
        return rule is null ? NotFound() : Ok(rule.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoutingRuleRequest request, CancellationToken ct)
    {
        var rule = await _ruleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule.ToDto());
    }

    [HttpPut("{id}/enable")]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var found = await _ruleService.EnableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/disable")]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var found = await _ruleService.DisableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/priority")]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdatePriorityRequest request, CancellationToken ct)
    {
        var found = await _ruleService.UpdatePriorityAsync(id, request.Priority, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var found = await _ruleService.DeleteAsync(id, ct);
        return found ? NoContent() : NotFound();
    }
}
