using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.Equipment;
using Dicom.Edge.Hub.Domain.Aggregates.Equipment;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>
/// Manages the equipment catalog scoped to a specific Edge Node. Each equipment carries
/// the set of allowed modality codes used by the node to filter the Modality Worklist
/// per device. Writes are pushed to the node automatically.
/// </summary>
[ApiController]
[Route("api/nodes/{nodeId}/equipment")]
[Authorize(Policy = Policies.ViewConfiguration)]
[EnableRateLimiting("api")]
public sealed class NodeEquipmentController(
    INodeEquipmentService equipmentService,
    INodeEquipmentRepository equipmentRepository,
    IOptions<EquipmentPresenceOptions> presenceOptions) : ControllerBase
{
    private TimeSpan OnlineWindow => presenceOptions.Value.OnlineWindow;

    [HttpGet]
    public async Task<IActionResult> GetByNode(string nodeId, CancellationToken ct)
    {
        var equipment = await equipmentRepository.GetByNodeIdAsync(nodeId, ct);
        return Ok(equipment.Select(e => e.ToDto(OnlineWindow)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string nodeId, string id, CancellationToken ct)
    {
        var equipment = await equipmentRepository.GetByIdAsync(id, ct);
        if (equipment is null || equipment.NodeId != nodeId) return NotFound();
        return Ok(equipment.ToDto(OnlineWindow));
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageModalities)]
    public async Task<IActionResult> Create(
        string nodeId,
        [FromBody] CreateNodeEquipmentRequest request,
        CancellationToken ct)
    {
        try
        {
            var equipment = await equipmentService.CreateAsync(nodeId, request, ct);
            return CreatedAtAction(nameof(GetById), new { nodeId, id = equipment.Id }, equipment.ToDto(OnlineWindow));
        }
        catch (UnsupportedModalityCodesException ex)
        {
            return BadRequest(new { error = ex.Message, codes = ex.Codes });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.ManageModalities)]
    public async Task<IActionResult> Update(
        string nodeId, string id,
        [FromBody] UpdateNodeEquipmentRequest request,
        CancellationToken ct)
    {
        try
        {
            var equipment = await equipmentService.UpdateAsync(id, request, ct);
            if (equipment is null || equipment.NodeId != nodeId) return NotFound();
            return Ok(equipment.ToDto(OnlineWindow));
        }
        catch (UnsupportedModalityCodesException ex)
        {
            return BadRequest(new { error = ex.Message, codes = ex.Codes });
        }
    }

    [HttpPut("{id}/enable")]
    [Authorize(Policy = Policies.ManageModalities)]
    public async Task<IActionResult> Enable(string nodeId, string id, CancellationToken ct)
    {
        var found = await equipmentService.EnableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPut("{id}/disable")]
    [Authorize(Policy = Policies.ManageModalities)]
    public async Task<IActionResult> Disable(string nodeId, string id, CancellationToken ct)
    {
        var found = await equipmentService.DisableAsync(id, ct);
        return found ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.ManageModalities)]
    public async Task<IActionResult> Delete(string nodeId, string id, CancellationToken ct)
    {
        var found = await equipmentService.DeleteAsync(id, ct);
        return found ? NoContent() : NotFound();
    }
}
