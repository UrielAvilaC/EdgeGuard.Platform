using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly INodeRepository _nodeRepository;
    private readonly INodeService _nodeService;
    private readonly ILogger<NodesController> _logger;

    public NodesController(
        INodeRepository nodeRepository,
        INodeService nodeService,
        ILogger<NodesController> logger)
    {
        _nodeRepository = nodeRepository;
        _nodeService = nodeService;
        _logger = logger;
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var pagination = new PaginationRequest { Page = page, PageSize = pageSize };
        var result = await _nodeRepository.GetPagedAsync(pagination, ct);
        return Ok(result.ToPagedResponse(n => n.ToDto()));
    }

    [HttpGet]
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
    public async Task<IActionResult> Create([FromBody] CreateNodeRequest request, CancellationToken ct)
    {
        var node = await _nodeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = node.Id }, node.ToDto());
    }

    [HttpPut("{id}/enable")]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var found = await _nodeService.EnableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/disable")]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var found = await _nodeService.DisableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var count = await _nodeRepository.CountAsync(ct);
        return Ok(new CountDto { Count = count });
    }
}
