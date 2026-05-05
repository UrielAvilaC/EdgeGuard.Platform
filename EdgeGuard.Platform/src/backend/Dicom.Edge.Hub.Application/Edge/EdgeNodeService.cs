using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Models.Enums;
using Dicom.Edge.Security.Authentication;
using Dicom.Edge.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Dicom.Edge.Hub.Application.Edge;

/// <summary>
/// Application service implementing all Edge (node-facing) business orchestration.
/// Extracted from EdgeController to enforce single-responsibility.
/// </summary>
public sealed class EdgeNodeService(
    INodeRepository nodeRepository,
    IStudyRepository studyRepository,
    IHealthCheckRepository healthCheckRepository,
    INodeTelemetryRepository telemetryRepository,
    IPacsServerRepository pacsRepository,
    INodeConfigurationService configService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    ILogger<EdgeNodeService> logger) : IEdgeNodeService
{
    public async Task<NodeRegistrationResponse> RegisterAsync(
        NodeRegistrationRequest request, CancellationToken ct = default)
    {
        var existing = await nodeRepository.GetByAeTitleAsync(request.AeTitle, ct);
        if (existing is not null)
        {
            logger.LogInformation("Node re-registration: {AeTitle} ({NodeId})", request.AeTitle, existing.Id);
            existing.UpdateHeartbeat();
            existing.UpdateConfiguration(
                location: request.Location,
                facilityName: request.FacilityName,
                version: request.Version);

            await nodeRepository.UpdateAsync(existing, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Re-registration: do NOT return API key again
            return new NodeRegistrationResponse
            {
                NodeId = existing.Id,
                Accepted = true,
                Message = "Re-registered successfully. API key unchanged."
            };
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

        // Generate API key, hash it, store the hash
        var rawApiKey = ApiKeyGenerator.Generate();
        var apiKeyHash = passwordHasher.HashPassword(rawApiKey);
        node.SetApiKeyHash(apiKeyHash);

        await nodeRepository.AddAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Node registered: {NodeId} {AeTitle} at {Ip}:{Port} (API key issued)",
            node.Id, request.AeTitle, request.IpAddress, request.Port);

        // First registration: return API key (one time only)
        return new NodeRegistrationResponse
        {
            NodeId = node.Id,
            Accepted = true,
            Message = "Registered successfully. Store the API key securely — it will not be shown again.",
            ApiKey = rawApiKey
        };
    }

    public async Task<EdgeOperationResult?> ProcessHeartbeatAsync(
        NodeHeartbeatRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        node.UpdateHeartbeat(
            availableStorageMb: request.AvailableStorageMb,
            totalStudiesReceived: request.TotalStudiesReceived,
            totalStudiesSent: request.TotalStudiesSent,
            errorsLast24Hours: request.ErrorsLast24Hours);

        await nodeRepository.UpdateAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new EdgeOperationResult(true, DateTime.UtcNow);
    }

    public async Task<EdgeStudyNotifyResult?> ProcessStudyNotifyAsync(
        StudyNotifyRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        var existing = await studyRepository.GetByStudyInstanceUidAsync(request.StudyInstanceUid, ct);
        if (existing is not null)
        {
            existing.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes);
            await studyRepository.UpdateAsync(existing, ct);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Study updated from node {NodeId}: StudyUID={StudyUid} Instances={Count}",
                request.NodeId, request.StudyInstanceUid, request.InstanceCount);

            return new EdgeStudyNotifyResult(true, existing.Id, DateTime.UtcNow);
        }

        var study = Study.Create(
            DicomUid.Create(request.StudyInstanceUid),
            patientId: request.PatientId,
            patientName: request.PatientName,
            sourceNodeId: request.NodeId,
            accessionNumber: request.AccessionNumber);

        study.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes);

        await studyRepository.AddAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Study created from node {NodeId}: StudyUID={StudyUid} Patient={Patient}",
            request.NodeId, request.StudyInstanceUid, request.PatientName);

        return new EdgeStudyNotifyResult(true, study.Id, DateTime.UtcNow);
    }

    public async Task<EdgeOperationResult?> ProcessHealthReportAsync(
        NodeHealthReportRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        var record = HealthCheckRecord.Create(
            request.NodeId,
            NodeStatus.Online,
            cpuUsagePercent: request.CpuPercent,
            diskAvailableMb: request.AvailableStorageMb,
            memoryUsageMb: request.MemoryPercent is not null ? (long)request.MemoryPercent : null,
            queuedStudies: request.QueueDepth);

        await healthCheckRepository.AddAsync(record, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Health report persisted from node {NodeId}: Storage={AvailMb}MB, CPU={Cpu}%, Mem={Mem}%",
            request.NodeId, request.AvailableStorageMb, request.CpuPercent, request.MemoryPercent);

        return new EdgeOperationResult(true, DateTime.UtcNow);
    }

    public async Task<EdgeOperationResult?> ProcessTelemetryAsync(
        NodeTelemetryRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        var record = NodeTelemetryRecord.Create(
            request.NodeId,
            DateTime.UtcNow,
            request.PeriodStart,
            request.PeriodEnd,
            request.TotalAssociations,
            request.AcceptedAssociations,
            request.RejectedAssociations,
            request.AbortedAssociations,
            request.TotalImagesReceived,
            request.CompletedStudies,
            request.TotalBytesReceived,
            request.AverageReceptionDurationMs,
            request.AverageThroughputMbps);

        await telemetryRepository.AddAsync(record, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Telemetry received from node {NodeId}: Associations={Total} Images={Images} Studies={Studies}",
            request.NodeId, request.TotalAssociations, request.TotalImagesReceived, request.CompletedStudies);

        return new EdgeOperationResult(true, DateTime.UtcNow);
    }

    public async Task<Dictionary<string, string>?> PullConfigurationAsync(
        string nodeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return null;

        var node = await nodeRepository.GetWithPacsAssignmentsAsync(nodeId, ct);
        if (node is null) return null;

        var config = await configService.GetNodeConfigAsync(nodeId, ct);
        var dict = config.ToDictionary(c => c.SettingKey, c => c.Value);

        // Inject active PACS assignments as a JSON array under cecho.destinations
        var activePacsIds = node.PacsAssignments
            .Where(a => a.IsActive)
            .Select(a => a.PacsId)
            .ToList();

        if (activePacsIds.Count > 0)
        {
            var pacsServers = await pacsRepository.GetAllAsync(ct);
            var destinations = pacsServers
                .Where(p => p.IsEnabled && activePacsIds.Contains(p.Id))
                .Select(p => new
                {
                    Id      = p.Id,
                    AeTitle = p.AeTitle.Value,
                    Host    = p.HostName,
                    Port    = p.Port,
                    UseTls  = false
                })
                .ToArray();

            dict["cecho.destinations"] = JsonSerializer.Serialize(destinations);

            // Also set single-destination sender keys to the first active PACS
            var primary = destinations[0];
            dict["pacs_dest.host"]     = primary.Host;
            dict["pacs_dest.port"]     = primary.Port.ToString();
            dict["pacs_dest.ae_title"] = primary.AeTitle;
        }

        logger.LogInformation(
            "Config pull by node {NodeId}: {Count} entries, {PacsCount} PACS destination(s)",
            nodeId, dict.Count, activePacsIds.Count);

        return dict;
    }
}
