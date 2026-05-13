using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>
/// Manages per-node configuration profiles and triggers on-demand config push.
/// </summary>
[ApiController]
[Route("api/node-configuration")]
[Authorize(Policy = Policies.ViewConfiguration)]
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
    /// GET /api/node-configuration/{nodeId}/categories — Returns distinct setting categories for a node.
    /// </summary>
    [HttpGet("{nodeId}/categories")]
    public async Task<IActionResult> GetCategories(string nodeId, CancellationToken ct)
    {
        var categories = await _configService.GetCategoriesAsync(nodeId, ct);
        return Ok(categories);
    }

    /// <summary>
    /// PUT /api/node-configuration/{nodeId}/batch — Saves multiple settings in one call (Hub DB only).
    /// Call POST /{nodeId}/push afterwards to propagate changes to the node.
    /// </summary>
    [HttpPut("{nodeId}/batch")]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> UpdateBatch(
        string nodeId, [FromBody] BatchUpdateNodeSettingsRequest request, CancellationToken ct)
    {
        var result = await _configService.UpdateBatchAsync(nodeId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/node-configuration/{nodeId}/reset-category/{category} — Resets all settings in a category to defaults.
    /// </summary>
    [HttpPost("{nodeId}/reset-category/{category}")]
    [Authorize(Policy = Policies.ManageEdgeNodes)]
    public async Task<IActionResult> ResetCategory(string nodeId, string category, CancellationToken ct)
    {
        var count = await _configService.ResetCategoryAsync(nodeId, category, ct);
        return Ok(new { resetCount = count });
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
            return BadRequest(new ErrorDto { Error = result.Error ?? "Push failed" });

        return Ok(new ConfigPushResultDto
        {
            AppliedVersion = result.AppliedVersion,
            UpdatedCount = result.UpdatedCount
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
