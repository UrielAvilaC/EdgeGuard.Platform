using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.Edge;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>
/// Node-facing endpoints. Edge Nodes call these endpoints to register,
/// send heartbeats, and report status to the Hub.
/// All endpoints except /edge/register require API key authentication.
/// /edge/register is protected by bootstrap token middleware.
/// </summary>
[Route("api/edge")]
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.Scheme)]
public class EdgeController : ControllerBase
{
    private readonly IEdgeNodeService _edgeService;
    private readonly IBootstrapTokenService _bootstrapTokenService;
    private readonly ILogger<EdgeController> _logger;

    public EdgeController(
        IEdgeNodeService edgeService,
        IBootstrapTokenService bootstrapTokenService,
        ILogger<EdgeController> logger)
    {
        _edgeService           = edgeService;
        _bootstrapTokenService = bootstrapTokenService;
        _logger                = logger;
    }

    /// <summary>POST /edge/register — Node self-registration. Protected by bootstrap token middleware.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] NodeRegistrationRequest request, CancellationToken ct)
    {
        var result = await _edgeService.RegisterAsync(request, ct);

        // Consume the one-time token after successful registration (first-time only)
        if (!string.IsNullOrEmpty(result.ApiKey)
            && HttpContext.Items["ValidatedBootstrapToken"] is NodeBootstrapToken bootstrapToken)
        {
            await _bootstrapTokenService.ConsumeAsync(bootstrapToken, result.NodeId, ct);
        }

        return result.Message?.Contains("Re-registered") == true
            ? Ok(result)
            : CreatedAtAction(null, result);
    }

    /// <summary>
    /// POST /edge/token — Self-service bootstrap token for unregistered nodes.
    /// No authentication required. Token valid for 5 minutes, one-time use.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestToken(CancellationToken ct)
    {
        var result = await _bootstrapTokenService.GenerateAsync(
            new CreateBootstrapTokenRequest { ExpiresInHours = 0, Note = "self-service" },
            createdByUserId: null,
            ct);
        return Ok(result);
    }

    /// <summary>POST /edge/heartbeat — Periodic heartbeat from a node.</summary>
    [HttpPost("/api/edge/heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] NodeHeartbeatRequest request, CancellationToken ct)
    {
        var result = await _edgeService.ProcessHeartbeatAsync(request, ct);
        if (result is null)
            return NotFound(new ErrorDto { Error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, request.NodeId) });

        return Ok(new HeartbeatAckDto { Acknowledged = result.Acknowledged, ServerTimeUtc = result.ServerTimeUtc });
    }

    /// <summary>POST /edge/studies — Node notifies Hub of a received study.</summary>
    [HttpPost("studies")]
    public async Task<IActionResult> StudyNotify([FromBody] StudyNotifyRequest request, CancellationToken ct)
    {
        var result = await _edgeService.ProcessStudyNotifyAsync(request, ct);
        if (result is null)
            return NotFound(new ErrorDto { Error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, request.NodeId) });

        return Ok(new StudyNotifyAckDto
        {
            Acknowledged = result.Acknowledged,
            StudyId = result.StudyId,
            ReceivedAtUtc = result.ReceivedAtUtc
        });
    }

    /// <summary>POST /edge/health — Node reports health metrics.</summary>
    [HttpPost("health")]
    public async Task<IActionResult> HealthReport([FromBody] NodeHealthReportRequest request, CancellationToken ct)
    {
        var result = await _edgeService.ProcessHealthReportAsync(request, ct);
        if (result is null)
            return NotFound(new ErrorDto { Error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, request.NodeId) });

        return Ok(new HealthReportAckDto { Acknowledged = result.Acknowledged });
    }

    /// <summary>GET /edge/configuration — Node pulls its config as key-value pairs.</summary>
    [HttpGet("configuration")]
    public async Task<IActionResult> PullConfiguration([FromQuery] string nodeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return BadRequest(new ErrorDto { Error = "nodeId query parameter is required." });

        var config = await _edgeService.PullConfigurationAsync(nodeId, ct);
        if (config is null)
            return NotFound(new ErrorDto { Error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, nodeId) });

        return Ok(config);
    }

    /// <summary>GET /info — Hub version and capability info.</summary>
    [HttpGet("/api/info")]
    [AllowAnonymous]
    public IActionResult GetInfo()
    {
        return Ok(EdgeMappingProfile.ToHubInfo());
    }
}
