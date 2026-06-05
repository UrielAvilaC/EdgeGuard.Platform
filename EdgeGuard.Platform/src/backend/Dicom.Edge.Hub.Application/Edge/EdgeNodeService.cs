using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
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
using NodeRegistrationResponse = Dicom.Edge.Contracts.Hub.NodeRegistrationResponse;
using HubNodeRegistrationRequest = Dicom.Edge.Contracts.Hub.NodeRegistrationRequest;

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
    INodePacsEchoStore pacsEchoStore,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    ILogger<EdgeNodeService> logger) : IEdgeNodeService
{
    public async Task<NodeRegistrationResponse> RegisterAsync(
        HubNodeRegistrationRequest request, CancellationToken ct = default)
    {
        // Re-registration discovery uses Name + IpAddress. The Hub DOES persist the
        // node AE (Node.AeTitle); the Node derives it from its single source of truth
        // (DicomServer:AeTitle) and reports it here at registration.
        var existing = await nodeRepository.GetByNameAndIpAsync(request.Name, request.IpAddress, ct);
        if (existing is not null)
        {
            logger.LogInformation("Node re-registration: {Name} ({NodeId}) at {Ip}",
                request.Name, existing.Id, request.IpAddress);
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

        logger.LogInformation("Node registered: {NodeId} {Name} at {Ip}:{Port} (API key issued)",
            node.Id, request.Name, request.IpAddress, request.Port);

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
        logger.LogInformation(
            "Study notify received from node {NodeId}: StudyUID={StudyUid} Patient={Patient} Instances={Count}",
            request.NodeId, request.StudyInstanceUid, request.PatientName, request.InstanceCount);
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        var existing = await studyRepository.GetByStudyInstanceUidAsync(request.StudyInstanceUid, ct);

        // ── HL7 → DICOM fusion ────────────────────────────────────────────────
        // When an ORM arrives first, the study is created with a synthetic UID
        // keyed only by AccessionNumber. The real DICOM UID is unknown until the
        // Edge Node notifies after receiving the actual images. If the UID lookup
        // misses, fall back to AccessionNumber and promote the scheduled record.
        if (existing is null && !string.IsNullOrWhiteSpace(request.AccessionNumber))
        {
            var scheduled = await studyRepository.GetByAccessionNumberAsync(request.AccessionNumber, ct);
            if (scheduled is not null && scheduled.Status == StudyStatus.Scheduled)
            {
                logger.LogInformation(
                    "Merging HL7-scheduled study {StudyId} (AccessionNumber={Accession}) " +
                    "with real DICOM UID {StudyUid} from node {NodeId}",
                    scheduled.Id, request.AccessionNumber, request.StudyInstanceUid, request.NodeId);

                scheduled.MergeFromDicom(
                    DicomUid.Create(request.StudyInstanceUid),
                    sourceNodeId: request.NodeId);

                existing = scheduled;
            }
        }
        // ─────────────────────────────────────────────────────────────────────

        if (existing is not null)
        {
            existing.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes, seriesCount: request.SeriesCount);
            existing.UpdateStudyMetadata(request.StudyDate, request.StudyDescription);
            existing.MarkCompleted();
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
            accessionNumber: request.AccessionNumber,
            studyDate: request.StudyDate,
            studyDescription: request.StudyDescription);

        study.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes, seriesCount: request.SeriesCount);
        study.MarkCompleted();

        await studyRepository.AddAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Study created from node {NodeId}: StudyUID={StudyUid} Patient={Patient}",
            request.NodeId, request.StudyInstanceUid, request.PatientName);

        return new EdgeStudyNotifyResult(true, study.Id, DateTime.UtcNow);
    }

    public async Task<EdgeStudyNotifyResult?> ProcessStudyProgressAsync(
        StudyProgressNotifyRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        var existing = await studyRepository.GetByStudyInstanceUidAsync(request.StudyInstanceUid, ct);

        // ── HL7 → DICOM fusion (incremental path) ────────────────────────────
        if (existing is null && !string.IsNullOrWhiteSpace(request.AccessionNumber))
        {
            var scheduled = await studyRepository.GetByAccessionNumberAsync(request.AccessionNumber, ct);
            if (scheduled is not null && scheduled.Status == StudyStatus.Scheduled)
            {
                logger.LogInformation(
                    "Progress merge: HL7-scheduled study {StudyId} (AccessionNumber={Accession}) " +
                    "promoted to Receiving with real UID {StudyUid} from node {NodeId}",
                    scheduled.Id, request.AccessionNumber, request.StudyInstanceUid, request.NodeId);

                scheduled.MergeFromDicom(
                    DicomUid.Create(request.StudyInstanceUid),
                    sourceNodeId: request.NodeId);

                existing = scheduled;
            }
        }
        // ─────────────────────────────────────────────────────────────────────

        if (existing is not null)
        {
            existing.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes, seriesCount: request.SeriesCount);
            existing.UpdateStudyMetadata(request.StudyDate, request.StudyDescription);
            await studyRepository.UpdateAsync(existing, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return new EdgeStudyNotifyResult(true, existing.Id, DateTime.UtcNow);
        }

        var study = Study.Create(
            DicomUid.Create(request.StudyInstanceUid),
            patientId: request.PatientId,
            patientName: request.PatientName,
            sourceNodeId: request.NodeId,
            accessionNumber: request.AccessionNumber,
            studyDate: request.StudyDate,
            studyDescription: request.StudyDescription);

        study.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes, seriesCount: request.SeriesCount);

        await studyRepository.AddAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogDebug(
            "Study created via progress from node {NodeId}: StudyUID={StudyUid}",
            request.NodeId, request.StudyInstanceUid);

        return new EdgeStudyNotifyResult(true, study.Id, DateTime.UtcNow);
    }

    public async Task<EdgeOperationResult?> ProcessHealthReportAsync(
        NodeHealthReportRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        // A health report is proof of life — refresh the heartbeat timestamp.
        node.UpdateHeartbeat(availableStorageMb: request.AvailableStorageMb);
        await nodeRepository.UpdateAsync(node, ct);

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

        // Telemetry is proof of life — refresh the heartbeat timestamp.
        node.UpdateHeartbeat();
        await nodeRepository.UpdateAsync(node, ct);

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

            dict[SharedNodeSettingKeys.PacsCEcho.Destinations] =
                JsonSerializer.Serialize(destinations);
        }

        logger.LogInformation(
            "Config pull by node {NodeId}: {Count} entries, {PacsCount} PACS destination(s)",
            nodeId, dict.Count, activePacsIds.Count);

        return dict;
    }

    public Task<EdgeOperationResult?> ProcessPacsEchoReportAsync(
        NodePacsEchoReportRequest request, CancellationToken ct = default)
    {
        var node = nodeRepository.GetByIdAsync(request.NodeId, ct);
        // We don't await the existence check — just store optimistically.
        // If the node disappears it's a cosmetic issue.
        var status = new NodePacsCEchoStatusDto
        {
            NodeId        = request.NodeId,
            ReportedAtUtc = request.ReportedAtUtc,
            Destinations  = request.Results.Select(r => new PacsCEchoDestinationDto
            {
                AeTitle     = r.AeTitle,
                Host        = r.Host,
                Port        = r.Port,
                Success     = r.Success,
                LatencyMs   = r.LatencyMs,
                Error       = r.Error,
                ErrorReason = r.ErrorReason,
                CheckedAtUtc = r.CheckedAtUtc,
            }).ToList().AsReadOnly(),
        };

        pacsEchoStore.Upsert(status);

        logger.LogDebug(
            "PACS echo report stored for node {NodeId}: {Count} destination(s), {Ok} reachable",
            request.NodeId, status.TotalChecked, status.TotalReachable);

        return Task.FromResult<EdgeOperationResult?>(new EdgeOperationResult(true, DateTime.UtcNow));
    }
}
