using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Application.Configuration;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/system-settings")]
[Authorize(Policy = Policies.ViewConfiguration)]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settingsService;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(
        ISystemSettingsService settingsService,
        ILogger<SystemSettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var settings = await _settingsService.GetAllAsync(ct);
        return Ok(settings);
    }

    [HttpGet("by-category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        var settings = await _settingsService.GetByCategoryAsync(category, ct);
        return Ok(settings);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
    {
        var setting = await _settingsService.GetAsync(key, ct);
        return setting is null ? NotFound() : Ok(setting);
    }

    [HttpPut("{key}")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSettingRequest request, CancellationToken ct)
    {
        var existing = await _settingsService.GetAsync(key, ct);
        if (existing is null) return NotFound();

        await _settingsService.SetAsync(key, request.Value, ct);
        return NoContent();
    }

    [HttpPost("seed-defaults")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> SeedDefaults(CancellationToken ct)
    {
        await _settingsService.SeedDefaultsAsync(ct);
        return Ok(new MessageDto { Message = HubApiConstants.DefaultsSeededMessage });
    }
}