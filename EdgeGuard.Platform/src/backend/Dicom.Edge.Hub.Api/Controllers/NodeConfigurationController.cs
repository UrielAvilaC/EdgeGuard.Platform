using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>
/// Manages per-node configuration profiles and triggers on-demand config push.
/// </summary>
[ApiController]
[Route("api/node-configuration")]
public class NodeConfigurationController : ControllerBase
{
    private readonly INodeConfigurationService _configService;
    private readonly INodeConfigPushService _pushService;

    public NodeConfigurationController(
        INodeConfigurationService configService,
        INodeConfigPushService pushService)
    {
        _configService = configService;
        _pushService = pushService;
    }

    /// <summary>
    /// GET /api/node-configuration/{nodeId} — Returns all configuration entries for a node.
    /// </summary>
    [HttpGet("{nodeId}")]
    public async Task<IActionResult> GetAll(string nodeId, CancellationToken ct)
    {
        var config = await _configService.GetNodeConfigAsync(nodeId, ct);
        return Ok(config);
    }

    /// <summary>
    /// GET /api/node-configuration/{nodeId}/category/{category} — Returns config filtered by category.
    /// </summary>
    [HttpGet("{nodeId}/category/{category}")]
    public async Task<IActionResult> GetByCategory(string nodeId, string category, CancellationToken ct)
    {
        var config = await _configService.GetNodeConfigByCategoryAsync(nodeId, category, ct);
        return Ok(config);
    }

    /// <summary>
    /// PUT /api/node-configuration/{nodeId}/{settingKey} — Updates a single setting value.
    /// </summary>
    [HttpPut("{nodeId}/{settingKey}")]
    public async Task<IActionResult> UpdateSetting(
        string nodeId, string settingKey, [FromBody] UpdateNodeSettingRequest request, CancellationToken ct)
    {
        var updated = await _configService.UpdateSettingAsync(nodeId, settingKey, request.Value, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>
    /// POST /api/node-configuration/{nodeId}/reset/{settingKey} — Resets a setting to its default value.
    /// </summary>
    [HttpPost("{nodeId}/reset/{settingKey}")]
    public async Task<IActionResult> ResetSetting(string nodeId, string settingKey, CancellationToken ct)
    {
        var reset = await _configService.ResetSettingAsync(nodeId, settingKey, ct);
        return reset ? NoContent() : NotFound();
    }

    /// <summary>
    /// POST /api/node-configuration/{nodeId}/push — Triggers an on-demand config push to the node.
    /// </summary>
    [HttpPost("{nodeId}/push")]
    public async Task<IActionResult> PushConfig(string nodeId, CancellationToken ct)
    {
        var result = await _pushService.PushConfigAsync(nodeId, ct);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            appliedVersion = result.AppliedVersion,
            updatedCount = result.UpdatedCount
        });
    }

    /// <summary>
    /// GET /api/node-configuration/{nodeId}/version — Returns the computed config version hash.
    /// </summary>
    [HttpGet("{nodeId}/version")]
    public async Task<IActionResult> GetVersion(string nodeId, CancellationToken ct)
    {
        var version = await _configService.ComputeConfigVersionAsync(nodeId, ct);
        return Ok(new { configVersion = version });
    }
}
