using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.Routing;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/routing-rules")]
[Authorize(Policy = Policies.ViewConfiguration)]
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
    public async Task<IActionResult> GetPaged([FromQuery] RoutingRuleFilter filter, CancellationToken ct)
    {
        var pagination = new PaginationRequest { Page = filter.Page, PageSize = filter.PageSize };
        var criteria = new RoutingRuleFilterCriteria
        {
            Search = filter.Search,
            IsEnabled = filter.IsEnabled,
            TargetNodeId = filter.TargetNodeId,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };
        var result = await _ruleRepository.GetFilteredPagedAsync(pagination, criteria, ct);
        return Ok(result.ToPagedResponse(r => r.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, ct);
        return rule is null ? NotFound() : Ok(rule.ToDto());
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Create([FromBody] CreateRoutingRuleRequest request, CancellationToken ct)
    {
        var rule = await _ruleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRoutingRuleRequest request, CancellationToken ct)
    {
        var rule = await _ruleService.UpdateAsync(id, request, ct);
        return rule is null ? NotFound() : Ok(rule.ToDto());
    }

    [HttpPut("{id}/enable")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var found = await _ruleService.EnableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/disable")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var found = await _ruleService.DisableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/priority")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> UpdatePriority(string id, [FromBody] UpdatePriorityRequest request, CancellationToken ct)
    {
        var found = await _ruleService.UpdatePriorityAsync(id, request.Priority, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.ManageRoutingRules)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var found = await _ruleService.DeleteAsync(id, ct);
        return found ? NoContent() : NotFound();
    }
}
