using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>Notification operational settings: auto-mode toggle + SMTP status/test.</summary>
[ApiController]
[Route("api/notification-settings")]
[Authorize(Policy = Policies.ViewConfiguration)]
[EnableRateLimiting("api")]
public class NotificationSettingsController(INotificationSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await service.GetAsync(ct));

    [HttpPut("auto-mode")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> SetAutoMode([FromBody] SetAutoModeRequest request, CancellationToken ct)
    {
        await service.SetAutoModeAsync(request.AutoMode, ct);
        return NoContent();
    }

    [HttpPost("smtp/test")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> TestSmtp([FromBody] TestSmtpRequest request, CancellationToken ct) =>
        Ok(await service.TestSmtpAsync(request.ToEmail, ct));
}
