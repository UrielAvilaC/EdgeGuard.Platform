using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PacsServersController : ControllerBase
{
    private readonly IPacsServerRepository _pacsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PacsServersController(IPacsServerRepository pacsRepository, IUnitOfWork unitOfWork)
    {
        _pacsRepository = pacsRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var servers = await _pacsRepository.GetAllAsync(ct);
        return Ok(servers.Select(MapToDto));
    }

    [HttpGet("enabled")]
    public async Task<IActionResult> GetEnabled(CancellationToken ct)
    {
        var servers = await _pacsRepository.GetEnabledAsync(ct);
        return Ok(servers.Select(MapToDto));
    }

    [HttpGet("global")]
    public async Task<IActionResult> GetGlobal(CancellationToken ct)
    {
        var servers = await _pacsRepository.GetGlobalAsync(ct);
        return Ok(servers.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var pacs = await _pacsRepository.GetByIdAsync(id, ct);
        return pacs is null ? NotFound() : Ok(MapToDto(pacs));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePacsServerRequest request, CancellationToken ct)
    {
        var pacs = PacsServer.Create(
            request.Name,
            AeTitle.Create(request.AeTitle),
            request.HostName,
            request.Port,
            request.Description,
            request.IsGlobal,
            request.MaxConcurrentAssociations,
            request.TimeoutSeconds);

        await _pacsRepository.AddAsync(pacs, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = pacs.Id }, MapToDto(pacs));
    }

    [HttpPut("{id}/enable")]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var pacs = await _pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return NotFound();

        pacs.Enable();
        await _pacsRepository.UpdateAsync(pacs, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPut("{id}/disable")]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var pacs = await _pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return NotFound();

        pacs.Disable();
        await _pacsRepository.UpdateAsync(pacs, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _pacsRepository.DeleteAsync(id, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return NoContent();
    }

    private static object MapToDto(PacsServer p) => new
    {
        p.Id,
        p.Name,
        AeTitle = p.AeTitle.Value,
        p.HostName,
        p.Port,
        p.Description,
        p.IsEnabled,
        p.IsGlobal,
        p.MaxConcurrentAssociations,
        p.TimeoutSeconds,
        p.LastCEchoAt,
        p.LastCEchoSuccess,
        p.IsReachable,
        p.CreatedAt,
        p.UpdatedAt
    };
}

public sealed class CreatePacsServerRequest
{
    public required string Name { get; init; }
    public required string AeTitle { get; init; }
    public required string HostName { get; init; }
    public required int Port { get; init; }
    public string? Description { get; init; }
    public bool IsGlobal { get; init; }
    public int MaxConcurrentAssociations { get; init; } = 10;
    public int TimeoutSeconds { get; init; } = 30;
}
