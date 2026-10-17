using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Node.Persistence.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Api.Controllers;

/// <summary>
/// Receives configuration pushes from the Hub and reports the current config version.
/// </summary>
[ApiController]
[Route("api")]
public sealed class ConfigurationController(
    INodeSettingsService settingsService,
    ILogger<ConfigurationController> logger) : ControllerBase
{
    private const string ConfigSourceHub = "Hub";
    private const string ConfigVersionDefault = "not-initialized";

    /// <summary>
    /// POST /api/configuration/apply — Receives a full configuration snapshot from the Hub.
    /// </summary>
    [HttpPost("configuration/apply")]
    public async Task<IActionResult> Apply([FromBody] NodeConfigSyncDto payload, CancellationToken ct)
    {
        if (payload.Settings is null || payload.Settings.Count == 0)
        {
            return BadRequest(new ConfigSyncResultDto
            {
                Accepted = false,
                Error = "No settings provided in the payload.",
                AppliedAtUtc = DateTime.UtcNow
            });
        }

        try
        {
            logger.LogInformation(
                "Receiving config push from Hub. NodeId={NodeId}, Version={Version}, SettingCount={Count}",
                payload.NodeId, payload.ConfigVersion, payload.Settings.Count);

            await settingsService.ApplyBatchAsync(payload.Settings, ct);

            // La versión aplicada es lo único que le permite al Hub distinguir "el nodo
            // ya tiene esto" de "sigue pendiente". Va por RecordConfigSyncAsync y no por
            // SetAsync porque las filas system.* son solo-lectura para el Hub y SetAsync
            // las rechazaba en silencio: la configuración se aplicaba y la pantalla
            // seguía diciendo «Pendiente de aplicar» para siempre.
            await settingsService.RecordConfigSyncAsync(
                payload.ConfigVersion, ConfigSourceHub, ct);

            await settingsService.ReloadAsync(ct);

            logger.LogInformation(
                "Configuration applied successfully. Version={Version}, Applied={Count} settings",
                payload.ConfigVersion, payload.Settings.Count);

            return Ok(new ConfigSyncResultDto
            {
                Accepted       = true,
                AppliedVersion = payload.ConfigVersion,
                UpdatedCount   = payload.Settings.Count,
                AppliedAtUtc   = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply config push. Version={Version}", payload.ConfigVersion);

            return StatusCode(500, new ConfigSyncResultDto
            {
                Accepted = false,
                Error = ex.Message,
                AppliedAtUtc = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// GET /api/configuration/version — Returns the node's current config version hash.
    /// </summary>
    [HttpGet("configuration/version")]
    public async Task<IActionResult> GetVersion(CancellationToken ct)
    {
        var version = await settingsService.GetAsync<string>(
            NodeSettingKeys.System.ConfigVersion, ConfigVersionDefault, ct);

        return Ok(new { configVersion = version });
    }
}
