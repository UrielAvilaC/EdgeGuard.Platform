using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.Edge;
using Dicom.Edge.Hub.Application.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/nodes")]
[Authorize(Policy = Policies.ViewNodes)]
public class NodesController : ControllerBase
{
    private readonly INodeRepository _nodeRepository;
    private readonly INodeService _nodeService;
    private readonly IBootstrapTokenService _bootstrapTokenService;
    private readonly INodeTelemetryRepository _telemetryRepository;
    private readonly ILogger<NodesController> _logger;

    public NodesController(
        INodeRepository nodeRepository,
        INodeService nodeService,
        IBootstrapTokenService bootstrapTokenService,
        INodeTelemetryRepository telemetryRepository,
        ILogger<NodesController> logger)
    {
        _nodeRepository        = nodeRepository;
        _nodeService           = nodeService;
        _bootstrapTokenService = bootstrapTokenService;
        _telemetryRepository   = telemetryRepository;
        _logger                = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] NodeFilter filter, CancellationToken ct = default)
    {
        var pagination = new PaginationRequest { Page = filter.Page, PageSize = filter.PageSize };
        var criteria = new NodeFilterCriteria
        {
            Search = filter.Search,
            Status = filter.Status,
            IsEnabled = filter.IsEnabled,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };
        var result = await _nodeRepository.GetFilteredPagedAsync(pagination, criteria, ct);
        return Ok(result.ToPagedResponse(n => n.ToDto()));
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var nodes = await _nodeRepository.GetAllAsync(ct);
        return Ok(nodes.Select(n => n.ToDto()));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var nodes = await _nodeRepository.GetActiveNodesAsync(ct);
        return Ok(nodes.Select(n => n.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var node = await _nodeRepository.GetWithPacsAssignmentsAsync(id, ct);
        return node is null ? NotFound() : Ok(node.ToDto());
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> Create([FromBody] CreateNodeRequest request, CancellationToken ct)
    {
        var node = await _nodeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = node.Id }, node.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateNodeRequest request, CancellationToken ct)
    {
        var found = await _nodeService.UpdateAsync(id, request, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/enable")]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var found = await _nodeService.EnableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/disable")]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var found = await _nodeService.DisableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    // ── PACS Assignments ──────────────────────────────────────────────────────

    /// <summary>PUT /api/nodes/{id}/pacs/{pacsId} — Assigns a PACS server to a node.</summary>
    [HttpPut("{id}/pacs/{pacsId}")]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> AssignPacs(
        string id, string pacsId, [FromBody] AssignPacsRequest request, CancellationToken ct)
    {
        var found = await _nodeService.AssignPacsAsync(id, pacsId, request, ct);
        return found ? NoContent() : NotFound();
    }

    /// <summary>DELETE /api/nodes/{id}/pacs/{pacsId} — Removes a PACS assignment from a node.</summary>
    [HttpDelete("{id}/pacs/{pacsId}")]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> UnassignPacs(string id, string pacsId, CancellationToken ct)
    {
        var found = await _nodeService.UnassignPacsAsync(id, pacsId, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var count = await _nodeRepository.CountAsync(ct);
        return Ok(new CountDto { Count = count });
    }

    // ── Telemetry ────────────────────────────────────────────────────────────

    /// <summary>GET /api/nodes/{id}/telemetry — Returns the last N telemetry records for a node.</summary>
    [HttpGet("{id}/telemetry")]
    public async Task<IActionResult> GetTelemetry(
        string id,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var records = await _telemetryRepository.GetByNodeAsync(id, limit, ct);
        return Ok(records.Select(r => r.ToDto()));
    }

    // ── Bootstrap Tokens ─────────────────────────────────────────────

    /// <summary>
    /// POST /api/nodes/bootstrap-tokens — Generates a one-time bootstrap token.
    /// The raw token is returned once. The admin copies it to the node's appsettings.
    /// </summary>
    [HttpPost("bootstrap-tokens")]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> CreateBootstrapToken(
        [FromBody] CreateBootstrapTokenRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _bootstrapTokenService.GenerateAsync(request, userId, ct);
        return Ok(result);
    }
}
