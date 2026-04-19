using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.PacsServers;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewConfiguration)]
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
    public async Task<IActionResult> GetPaged([FromQuery] PacsServerFilter filter, CancellationToken ct)
    {
        var pagination = new PaginationRequest { Page = filter.Page, PageSize = filter.PageSize };
        var criteria = new PacsServerFilterCriteria
        {
            Search = filter.Search,
            IsEnabled = filter.IsEnabled,
            IsGlobal = filter.IsGlobal,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };
        var result = await _pacsRepository.GetFilteredPagedAsync(pagination, criteria, ct);
        return Ok(result.ToPagedResponse(p => p.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var pacs = await _pacsRepository.GetByIdAsync(id, ct);
        return pacs is null ? NotFound() : Ok(pacs.ToDto());
    }

    [HttpPost]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Create([FromBody] CreatePacsServerRequest request, CancellationToken ct)
    {
        var pacs = await _pacsService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = pacs.Id }, pacs.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePacsServerRequest request, CancellationToken ct)
    {
        var found = await _pacsService.UpdateAsync(id, request, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/enable")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var found = await _pacsService.EnableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/disable")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var found = await _pacsService.DisableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var found = await _pacsService.DeleteAsync(id, ct);
        return found ? NoContent() : NotFound();
    }
}
