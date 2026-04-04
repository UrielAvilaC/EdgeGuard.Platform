using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Models.Enums;
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
    private readonly IStudyRepository _studyRepository;
    private readonly IHealthCheckRepository _healthCheckRepository;
    private readonly INodeConfigurationService _configService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EdgeController> _logger;

    public EdgeController(
        INodeRepository nodeRepository,
        IStudyRepository studyRepository,
        IHealthCheckRepository healthCheckRepository,
        INodeConfigurationService configService,
        IUnitOfWork unitOfWork,
        ILogger<EdgeController> logger)
    {
        _nodeRepository = nodeRepository;
        _studyRepository = studyRepository;
        _healthCheckRepository = healthCheckRepository;
        _configService = configService;
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
        var node = await _nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null)
            return NotFound(new { error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, request.NodeId) });

        var existing = await _studyRepository.GetByStudyInstanceUidAsync(request.StudyInstanceUid, ct);
        if (existing is not null)
        {
            existing.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes);
            await _studyRepository.UpdateAsync(existing, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Study updated from node {NodeId}: StudyUID={StudyUid} Instances={Count}",
                request.NodeId, request.StudyInstanceUid, request.InstanceCount);

            return Ok(new { acknowledged = true, studyId = existing.Id, receivedAtUtc = DateTime.UtcNow });
        }

        var study = Study.Create(
            DicomUid.Create(request.StudyInstanceUid),
            patientId: request.PatientId,
            patientName: request.PatientName,
            sourceNodeId: request.NodeId,
            accessionNumber: request.AccessionNumber);

        study.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes);

        await _studyRepository.AddAsync(study, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Study created from node {NodeId}: StudyUID={StudyUid} Patient={Patient}",
            request.NodeId, request.StudyInstanceUid, request.PatientName);

        return Ok(new { acknowledged = true, studyId = study.Id, receivedAtUtc = DateTime.UtcNow });
    }

    /// <summary>POST /edge/health — Node reports health metrics.</summary>
    [HttpPost("/edge/health")]
    public async Task<IActionResult> HealthReport([FromBody] NodeHealthReportRequest request, CancellationToken ct)
    {
        var node = await _nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null)
            return NotFound(new { error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, request.NodeId) });

        var record = HealthCheckRecord.Create(
            request.NodeId,
            NodeStatus.Online,
            cpuUsagePercent: request.CpuPercent,
            diskAvailableMb: request.AvailableStorageMb,
            memoryUsageMb: request.MemoryPercent is not null ? (long)request.MemoryPercent : null,
            queuedStudies: request.QueueDepth);

        await _healthCheckRepository.AddAsync(record, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Health report persisted from node {NodeId}: Storage={AvailMb}MB, CPU={Cpu}%, Mem={Mem}%",
            request.NodeId, request.AvailableStorageMb, request.CpuPercent, request.MemoryPercent);

        return Ok(new { acknowledged = true });
    }

    /// <summary>GET /edge/configuration — Node pulls its config as key-value pairs.</summary>
    [HttpGet("/edge/configuration")]
    public async Task<IActionResult> PullConfiguration([FromQuery] string nodeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return BadRequest(new { error = "nodeId query parameter is required." });

        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null)
            return NotFound(new { error = string.Format(HubApiConstants.NodeNotRegisteredTemplate, nodeId) });

        var config = await _configService.GetNodeConfigAsync(nodeId, ct);
        var dict = config.ToDictionary(c => c.SettingKey, c => c.Value);

        _logger.LogInformation("Config pull by node {NodeId}: {Count} entries", nodeId, dict.Count);
        return Ok(dict);
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
