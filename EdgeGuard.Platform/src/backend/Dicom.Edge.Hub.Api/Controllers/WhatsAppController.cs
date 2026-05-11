using Dicom.Edge.Contracts.WhatsApp;
using Dicom.Edge.Hub.Application.WhatsApp;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
[Authorize(Policy = Policies.EditConfiguration)]
public class WhatsAppController(
    IWhatsAppNotificationService notificationService,
    IWhatsAppTemplateService templateService,
    IWhatsAppAutoSendRuleService ruleService,
    ILogger<WhatsAppController> logger) : ControllerBase
{
    // ── Config & Tags ────────────────────────────────────────────────────────

    [HttpGet("config-status")]
    public async Task<ActionResult<WhatsAppConfigStatusDto>> GetConfigStatus(CancellationToken ct) =>
        Ok(await notificationService.GetConfigStatusAsync(ct));

    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyList<WhatsAppTemplateTagDto>>> GetTags() =>
        Ok(await notificationService.GetAvailableTagsAsync());

    // ── Templates ────────────────────────────────────────────────────────────

    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<WhatsAppTemplateDto>>> GetTemplates(CancellationToken ct) =>
        Ok(await templateService.GetAllAsync(ct));

    [HttpGet("templates/{id}")]
    public async Task<ActionResult<WhatsAppTemplateDto>> GetTemplate(string id, CancellationToken ct)
    {
        var result = await templateService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("templates")]
    public async Task<ActionResult<WhatsAppTemplateDto>> CreateTemplate(
        [FromBody] CreateWhatsAppTemplateRequest request, CancellationToken ct)
    {
        var result = await templateService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetTemplate), new { id = result.Id }, result);
    }

    [HttpPut("templates/{id}")]
    public async Task<ActionResult<WhatsAppTemplateDto>> UpdateTemplate(
        string id, [FromBody] UpdateWhatsAppTemplateRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await templateService.UpdateAsync(id, request, ct));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(string id, CancellationToken ct)
    {
        try
        {
            await templateService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Auto-Send Rules ──────────────────────────────────────────────────────

    [HttpGet("auto-send-rules")]
    public async Task<ActionResult<IReadOnlyList<WhatsAppAutoSendRuleDto>>> GetRules(CancellationToken ct) =>
        Ok(await ruleService.GetAllAsync(ct));

    [HttpPost("auto-send-rules")]
    public async Task<ActionResult<WhatsAppAutoSendRuleDto>> CreateRule(
        [FromBody] CreateWhatsAppAutoSendRuleRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await ruleService.CreateAsync(request, ct));
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("auto-send-rules/{id}")]
    public async Task<ActionResult<WhatsAppAutoSendRuleDto>> UpdateRule(
        string id, [FromBody] UpdateWhatsAppAutoSendRuleRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await ruleService.UpdateAsync(id, request, ct));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("auto-send-rules/{id}")]
    public async Task<IActionResult> DeleteRule(string id, CancellationToken ct)
    {
        try
        {
            await ruleService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Manual Send & Tracking ───────────────────────────────────────────────

    [HttpPost("send")]
    public async Task<ActionResult<SendWhatsAppManualResponse>> SendManual(
        [FromBody] SendWhatsAppManualRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await notificationService.SendManualAsync(request, ct));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("notifications/by-study/{studyId}")]
    public async Task<ActionResult<IReadOnlyList<WhatsAppNotificationDto>>> GetByStudy(string studyId, CancellationToken ct) =>
        Ok(await notificationService.GetByStudyAsync(studyId, ct));
}
