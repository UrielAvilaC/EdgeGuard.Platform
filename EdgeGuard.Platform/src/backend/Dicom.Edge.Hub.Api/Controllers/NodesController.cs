using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly INodeRepository _nodeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NodesController(INodeRepository nodeRepository, IUnitOfWork unitOfWork)
    {
        _nodeRepository = nodeRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var nodes = await _nodeRepository.GetAllAsync(ct);
        return Ok(nodes.Select(n => MapToDto(n)));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var nodes = await _nodeRepository.GetActiveNodesAsync(ct);
        return Ok(nodes.Select(n => MapToDto(n)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var node = await _nodeRepository.GetWithPacsAssignmentsAsync(id, ct);
        return node is null ? NotFound() : Ok(MapToDto(node));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNodeRequest request, CancellationToken ct)
    {
        var node = Node.Create(
            request.Name,
            AeTitle.Create(request.AeTitle),
            request.IpAddress,
            request.Port,
            request.ApiEndpoint,
            request.Location,
            request.FacilityName,
            request.HealthCheckIntervalSeconds);

        await _nodeRepository.AddAsync(node, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = node.Id }, MapToDto(node));
    }

    [HttpPut("{id}/enable")]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var node = await _nodeRepository.GetByIdAsync(id, ct);
        if (node is null) return NotFound();

        node.Enable();
        await _nodeRepository.UpdateAsync(node, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPut("{id}/disable")]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var node = await _nodeRepository.GetByIdAsync(id, ct);
        if (node is null) return NotFound();

        node.Disable();
        await _nodeRepository.UpdateAsync(node, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var count = await _nodeRepository.CountAsync(ct);
        return Ok(new { count });
    }

    private static object MapToDto(Node n) => new
    {
        n.Id,
        n.Name,
        AeTitle = n.AeTitle.Value,
        n.IpAddress,
        n.Port,
        n.ApiEndpoint,
        n.Location,
        n.FacilityName,
        Status = n.Status.ToString(),
        n.IsEnabled,
        n.LastHeartbeatAt,
        n.HealthCheckIntervalSeconds,
        n.MaxStorageMb,
        n.AvailableStorageMb,
        n.TotalStudiesReceived,
        n.TotalStudiesSent,
        n.ErrorsLast24Hours,
        n.CreatedAt,
        n.UpdatedAt,
        PacsAssignments = n.PacsAssignments.Select(a => new { a.PacsId, a.IsActive, a.InheritedFromHub })
    };
}

public sealed class CreateNodeRequest
{
    public required string Name { get; init; }
    public required string AeTitle { get; init; }
    public required string IpAddress { get; init; }
    public required int Port { get; init; }
    public string? ApiEndpoint { get; init; }
    public string? Location { get; init; }
    public string? FacilityName { get; init; }
    public int HealthCheckIntervalSeconds { get; init; } = 60;
}
