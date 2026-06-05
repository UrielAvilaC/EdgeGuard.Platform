using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Security.Authorization;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>CRUD + preview + tag catalog for Email notification templates (enterprise editor).</summary>
[ApiController]
[Route("api/notification-templates")]
[Authorize(Policy = Policies.ViewConfiguration)]
[EnableRateLimiting("api")]
public class NotificationTemplatesController(
    INotificationTemplateService service,
    IHtmlSanitizer htmlSanitizer) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await service.GetAllAsync(ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var dto = await service.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("tags")]
    public IActionResult GetTags() => Ok(service.GetTags());

    [HttpPost("preview")]
    public IActionResult Preview([FromBody] PreviewTemplateRequest request)
    {
        var rendered = service.Preview(request);
        // Sanitize the HTML preview before returning (XSS protection).
        var body = string.Equals(request.Format, "Html", StringComparison.OrdinalIgnoreCase)
            ? htmlSanitizer.Sanitize(rendered.Body)
            : rendered.Body;
        return Ok(rendered with { Body = body });
    }

    [HttpPost]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationTemplateRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateNotificationTemplateRequest request, CancellationToken ct)
    {
        var dto = await service.UpdateAsync(id, request, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
