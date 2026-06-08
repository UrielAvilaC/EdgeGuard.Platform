using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Application.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/system-settings")]
[Authorize(Policy = Policies.ViewConfiguration)]
[EnableRateLimiting("api")]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settingsService;
    private readonly IRuntimeConfigReloader _configReloader;
    private readonly IHl7Listener _hl7Listener;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(
        ISystemSettingsService settingsService,
        IRuntimeConfigReloader configReloader,
        IHl7Listener hl7Listener,
        ILogger<SystemSettingsController> logger)
    {
        _settingsService = settingsService;
        _configReloader = configReloader;
        _hl7Listener = hl7Listener;
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

        // Validate HL7 bind keys before persisting so we never rebind into a bad value.
        if (key == HubSettingKeys.Hl7.TcpPort &&
            (!int.TryParse(request.Value, out var port) || port is < 1 or > 65535))
            return BadRequest("HL7 TCP port must be between 1 and 65535.");

        await _settingsService.SetAsync(key, request.Value, ct);

        // Hot-apply HL7 listener bind changes: reload config → restart listener → confirm.
        if (key is HubSettingKeys.Hl7.TcpPort or HubSettingKeys.Hl7.TcpEnabled)
        {
            _configReloader.Reload();
            _hl7Listener.RequestRestart();
            var applied = await ConfirmListenerAsync(key, request.Value, ct);

            if (!applied)
                _logger.LogWarning("HL7 listener did not reach the expected state after '{Key}' change", key);

            return Ok(new
            {
                applied,
                port = _hl7Listener.Port,
                running = _hl7Listener.IsRunning,
                warning = applied ? null : "El listener no alcanzó el estado esperado (¿puerto en uso?)."
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Polls (≤5s) for the HL7 listener to reach the state implied by the changed setting,
    /// so the operator gets immediate feedback (e.g. a port already in use).
    /// </summary>
    private async Task<bool> ConfirmListenerAsync(string key, string value, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            if (key == HubSettingKeys.Hl7.TcpEnabled)
            {
                var wantEnabled = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                if (_hl7Listener.IsRunning == wantEnabled) return true;
            }
            else if (key == HubSettingKeys.Hl7.TcpPort &&
                     int.TryParse(value, out var port) && _hl7Listener.Port == port && _hl7Listener.IsRunning)
            {
                return true;
            }

            await Task.Delay(200, ct);
        }
        return false;
    }

    [HttpPost("seed-defaults")]
    [Authorize(Policy = Policies.EditConfiguration)]
    public async Task<IActionResult> SeedDefaults(CancellationToken ct)
    {
        await _settingsService.SeedDefaultsAsync(ct);
        return Ok(new MessageDto { Message = HubApiConstants.DefaultsSeededMessage });
    }
}