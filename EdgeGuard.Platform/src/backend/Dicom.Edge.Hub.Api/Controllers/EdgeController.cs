using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>
/// Node-facing endpoints. Edge Nodes call these endpoints to register,
/// send heartbeats, and report status to the Hub.
/// Routes match <see cref="Dicom.Edge.Contracts.Edge.HubApiRoutes"/>.
/// </summary>
[ApiController]
public class EdgeController : ControllerBase
{
    private readonly INodeRepository _nodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EdgeController> _logger;

    public EdgeController(
        INodeRepository nodeRepository,
        IUnitOfWork unitOfWork,
        ILogger<EdgeController> logger)
    {
        _nodeRepository = nodeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>POST /edge/register — Node self-registration.</summary>
    [HttpPost("/edge/register")]
    public async Task<IActionResult> Register([FromBody] NodeRegistrationRequest request, CancellationToken ct)
    {
        var existing = await _nodeRepository.GetByAeTitleAsync(request.AeTitle, ct);
        if (existing is not null)
        {
            _logger.LogInformation("Node re-registration: {AeTitle} ({NodeId})", request.AeTitle, existing.Id);
            existing.UpdateHeartbeat();
            existing.UpdateConfiguration(
                location: request.Location,
                facilityName: request.FacilityName,
                version: request.Version);

            await _nodeRepository.UpdateAsync(existing, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Ok(new NodeRegistrationResponse
            {
                NodeId = existing.Id,
                Accepted = true,
                Message = HubApiConstants.ReRegisteredMessage
            });
        }

        var node = Node.Create(
            request.Name,
            AeTitle.Create(request.AeTitle),
            request.IpAddress,
            request.Port,
            request.ApiEndpoint,
            request.Location,
            request.FacilityName);

        node.UpdateConfiguration(version: request.Version);

        await _nodeRepository.AddAsync(node, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node registered: {NodeId} {AeTitle} at {Ip}:{Port}",
            node.Id, request.AeTitle, request.IpAddress, request.Port);

        return CreatedAtAction(null, new NodeRegistrationResponse
        {
            NodeId = node.Id,
            Accepted = true,
            Message = HubApiConstants.RegisteredMessage
        });
    }

    /// <summary>POST /edge/heartbeat — Periodic heartbeat from a node.</summary>
    [HttpPost("/edge/heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] NodeHeartbeatRequest request, CancellationToken ct)
    {
        var node = await _nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null)
            return NotFound(new { error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, request.NodeId) });

        node.UpdateHeartbeat(
            availableStorageMb: request.AvailableStorageMb,
            totalStudiesReceived: request.TotalStudiesReceived,
            totalStudiesSent: request.TotalStudiesSent,
            errorsLast24Hours: request.ErrorsLast24Hours);

        await _nodeRepository.UpdateAsync(node, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new
        {
            acknowledged = true,
            serverTimeUtc = DateTime.UtcNow
        });
    }

    /// <summary>POST /edge/studies — Node notifies Hub of a received study.</summary>
    [HttpPost("/edge/studies")]
    public async Task<IActionResult> StudyNotify([FromBody] StudyNotifyRequest request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Study notify from node {NodeId}: StudyUID={StudyUid} Patient={Patient}",
            request.NodeId, request.StudyInstanceUid, request.PatientName);

        return Ok(new
        {
            acknowledged = true,
            receivedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>POST /edge/health — Node reports health metrics.</summary>
    [HttpPost("/edge/health")]
    public async Task<IActionResult> HealthReport([FromBody] NodeHealthReportRequest request, CancellationToken ct)
    {
        var node = await _nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null)
            return NotFound(new { error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, request.NodeId) });

        _logger.LogInformation(
            "Health report from node {NodeId}: Storage={AvailMb}MB, CPU={Cpu}%, Mem={Mem}%",
            request.NodeId, request.AvailableStorageMb, request.CpuPercent, request.MemoryPercent);

        return Ok(new { acknowledged = true });
    }

    /// <summary>GET /info — Hub version and capability info.</summary>
    [HttpGet("/info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            service = HubApiConstants.ServiceDisplayName,
            version = HubApiConstants.ServiceVersion,
            utcNow = DateTime.UtcNow,
            capabilities = HubApiConstants.Capabilities
        });
    }
}

// ── Request/Response models ──────────────────────────────────────────────────

public sealed class NodeRegistrationRequest
{
    public required string Name { get; init; }
    public required string AeTitle { get; init; }
    public required string IpAddress { get; init; }
    public required int Port { get; init; }
    public string? ApiEndpoint { get; init; }
    public string? Location { get; init; }
    public string? FacilityName { get; init; }
    public string? Version { get; init; }
}

public sealed class NodeRegistrationResponse
{
    public required string NodeId { get; init; }
    public required bool Accepted { get; init; }
    public string? Message { get; init; }
}

public sealed class NodeHeartbeatRequest
{
    public required string NodeId { get; init; }
    public long? AvailableStorageMb { get; init; }
    public int? TotalStudiesReceived { get; init; }
    public int? TotalStudiesSent { get; init; }
    public int? ErrorsLast24Hours { get; init; }
}

public sealed class StudyNotifyRequest
{
    public required string NodeId { get; init; }
    public required string StudyInstanceUid { get; init; }
    public string? PatientId { get; init; }
    public string? PatientName { get; init; }
    public string? AccessionNumber { get; init; }
    public int InstanceCount { get; init; }
    public long TotalSizeBytes { get; init; }
}

public sealed class NodeHealthReportRequest
{
    public required string NodeId { get; init; }
    public long? AvailableStorageMb { get; init; }
    public double? CpuPercent { get; init; }
    public double? MemoryPercent { get; init; }
    public int? QueueDepth { get; init; }
}
