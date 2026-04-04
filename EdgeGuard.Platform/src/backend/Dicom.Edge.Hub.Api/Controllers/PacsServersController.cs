using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.PacsServers;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PacsServersController : ControllerBase
{
    private readonly IPacsServerRepository _pacsRepository;
    private readonly IPacsServerService _pacsService;
    private readonly ILogger<PacsServersController> _logger;

    public PacsServersController(
        IPacsServerRepository pacsRepository,
        IPacsServerService pacsService,
        ILogger<PacsServersController> logger)
    {
        _pacsRepository = pacsRepository;
        _pacsService = pacsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var servers = await _pacsRepository.GetAllAsync(ct);
        return Ok(servers.Select(p => p.ToDto()));
    }

    [HttpGet("enabled")]
    public async Task<IActionResult> GetEnabled(CancellationToken ct)
    {
        var servers = await _pacsRepository.GetEnabledAsync(ct);
        return Ok(servers.Select(p => p.ToDto()));
    }

    [HttpGet("global")]
    public async Task<IActionResult> GetGlobal(CancellationToken ct)
    {
        var servers = await _pacsRepository.GetGlobalAsync(ct);
        return Ok(servers.Select(p => p.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var pacs = await _pacsRepository.GetByIdAsync(id, ct);
        return pacs is null ? NotFound() : Ok(pacs.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePacsServerRequest request, CancellationToken ct)
    {
        var pacs = await _pacsService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = pacs.Id }, pacs.ToDto());
    }

    [HttpPut("{id}/enable")]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var found = await _pacsService.EnableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/disable")]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var found = await _pacsService.DisableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var found = await _pacsService.DeleteAsync(id, ct);
        return found ? NoContent() : NotFound();
    }
}
