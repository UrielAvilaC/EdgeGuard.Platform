using Dicom.Edge.Abstractions.Configuration;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.Equipment;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Application.Patients;
using Dicom.Edge.Hub.Application.Studies;
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
    INodeEquipmentRepository equipmentRepository,
    IPatientRegistrationService patientRegistration,
    IPasswordHasher passwordHasher,
    ISettingEncryptionService secretProtector,
    IUnitOfWork unitOfWork,
    IStudyRealtimeNotifier studyRealtimeNotifier,
    ILogger<EdgeNodeService> logger) : IEdgeNodeService
{
    /// <summary>
    /// Registers the study's patient in the Hub catalogue. A walk-in study never carries
    /// an HL7 order, so the node notification is the only chance to create the record.
    /// Never throws — a catalogue failure must not abort the study ingestion.
    /// </summary>
    private async Task<string?> EnsurePatientAsync(
        string? patientDicomId,
        string? patientName,
        DateTime? birthDate,
        string? sex,
        Node node,
        CancellationToken ct)
    {
        try
        {
            var patient = await patientRegistration.EnsurePatientAsync(
                new PatientRegistrationInput
                {
                    PatientDicomId  = patientDicomId,
                    PatientName     = patientName,
                    BirthDate       = birthDate is null ? null : DateOnly.FromDateTime(birthDate.Value),
                    Sex             = sex,
                    FacilitySource  = node.FacilityName,
                    CreatedByNodeId = node.Id,
                },
                PatientDataSource.Dicom,
                ct);

            return patient?.Id;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Patient registration failed for {PatientDicomId} from node {NodeId} — study ingestion continues",
                patientDicomId, node.Id);
            return null;
        }
    }

    private Task NotifyStatusChangedAsync(Study study, string nodeId, CancellationToken ct) =>
        studyRealtimeNotifier.StatusChangedAsync(
            new StudyStatusChange(study.Id, study.Status.ToString(), study.PatientName, nodeId, DateTime.UtcNow, study.PacsStatus.ToString()),
            ct);

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

            await ReconcileAeTitleAsync(existing.Id, request.AeTitle, ct);

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

        // Generate the API key and store both forms: the hash verifies inbound node→Hub
        // calls, the encrypted copy signs outbound Hub→Node pushes.
        var rawApiKey = ApiKeyGenerator.Generate();
        node.SetApiKey(
            passwordHasher.HashPassword(rawApiKey),
            secretProtector.Encrypt(rawApiKey));

        await nodeRepository.AddAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Los perfiles se crean aquí y no de forma perezosa al abrir la pantalla: sembrados
        // tarde, el AE nacía con el default genérico y el primer pull se lo empujaba de
        // vuelta al nodo, pisando el suyo. Sembrado en el alta, arranca siendo la verdad.
        await configService.InitializeNodeDefaultsAsync(
            node.Id,
            new Dictionary<string, string>
            {
                [SharedNodeSettingKeys.Dicom.AeTitle] = request.AeTitle
            },
            ct);

        logger.LogInformation("Node registered: {NodeId} {Name} at {Ip}:{Port} AE={AeTitle} (API key issued)",
            node.Id, request.Name, request.IpAddress, request.Port, request.AeTitle);

        // First registration: return API key (one time only)
        return new NodeRegistrationResponse
        {
            NodeId = node.Id,
            Accepted = true,
            Message = "Registered successfully. Store the API key securely — it will not be shown again.",
            ApiKey = rawApiKey
        };
    }

    /// <summary>
    /// Concilia el AE que el nodo acaba de reportar con el que el Hub tiene configurado
    /// en <c>dicom.ae_title</c>, que es la única fuente de verdad del AE del nodo.
    /// </summary>
    /// <remarks>
    /// Cuando los dos difieren y el Hub tiene un valor explícito, <b>gana el Hub</b>: es
    /// configuración deseada, igual que el resto de los settings, y el nodo se corrige en
    /// el siguiente pull. Se registra en warning porque la divergencia casi siempre
    /// significa que alguien tocó el AE directamente en el nodo, y eso conviene verlo.
    /// El caso contrario — perfil todavía en el default genérico — sí adopta lo del nodo:
    /// es lo que repara las instalaciones que hoy tienen "EDGE_NODE" sembrado de más.
    /// </remarks>
    private async Task ReconcileAeTitleAsync(
        string nodeId, string reportedAeTitle, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reportedAeTitle))
            return;

        // Idempotente: si el nodo todavía no tiene perfiles, nacen ya con el AE reportado
        // en vez del default genérico. Si ya los tiene, no hace nada.
        await configService.InitializeNodeDefaultsAsync(
            nodeId,
            new Dictionary<string, string>
            {
                [SharedNodeSettingKeys.Dicom.AeTitle] = reportedAeTitle
            },
            ct);

        var configured = await configService.GetSettingValueAsync(
            nodeId, SharedNodeSettingKeys.Dicom.AeTitle, ct);

        if (string.Equals(configured, reportedAeTitle, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(configured)
            || string.Equals(configured, NodeAeTitle.Default, StringComparison.OrdinalIgnoreCase))
        {
            await configService.UpdateSettingAsync(
                nodeId, SharedNodeSettingKeys.Dicom.AeTitle, reportedAeTitle, ct);

            logger.LogInformation(
                "Node {NodeId} AE adopted from registration: {AeTitle} (profile held '{Previous}')",
                nodeId, reportedAeTitle, configured);
            return;
        }

        logger.LogWarning(
            "Node {NodeId} re-registered reporting AE '{Reported}' but the Hub has '{Configured}' "
            + "configured — the Hub value wins and will be pushed on the next config sync",
            nodeId, reportedAeTitle, configured);
    }

    public async Task<EdgeOperationResult?> ProcessHeartbeatAsync(
        NodeHeartbeatRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        node.UpdateHeartbeat(
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

        // Register the patient before touching the study so the catalogue is populated
        // even for walk-in studies that never went through a worklist order.
        var patientRecordId = await EnsurePatientAsync(
            request.PatientId, request.PatientName, request.PatientBirthDate, request.PatientSex, node, ct);

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

            // Self-repair: studies created before the patient existed (or before the FK)
            // pick up the link on their next notification.
            if (patientRecordId is not null) existing.LinkToPatientRecord(patientRecordId);

            await studyRepository.UpdateAsync(existing, ct);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Study updated from node {NodeId}: StudyUID={StudyUid} Instances={Count}",
                request.NodeId, request.StudyInstanceUid, request.InstanceCount);

            await NotifyStatusChangedAsync(existing, request.NodeId, ct);
            return new EdgeStudyNotifyResult(true, existing.Id, DateTime.UtcNow);
        }

        var study = Study.Create(
            DicomUid.Create(request.StudyInstanceUid),
            patientId: request.PatientId,
            patientName: request.PatientName,
            sourceNodeId: request.NodeId,
            accessionNumber: request.AccessionNumber,
            studyDate: request.StudyDate,
            studyDescription: request.StudyDescription,
            patientRecordId: patientRecordId);

        study.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes, seriesCount: request.SeriesCount);
        study.MarkCompleted();

        await studyRepository.AddAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Study created from node {NodeId}: StudyUID={StudyUid} Patient={Patient}",
            request.NodeId, request.StudyInstanceUid, request.PatientName);

        await NotifyStatusChangedAsync(study, request.NodeId, ct);
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

        // First progress report for this study — the patient may not be in the catalogue
        // yet. Deliberately not done on the update path above: progress fires per C-STORE.
        var newStudyPatientRecordId = await EnsurePatientAsync(
            request.PatientId, request.PatientName, request.PatientBirthDate, request.PatientSex, node, ct);

        var study = Study.Create(
            DicomUid.Create(request.StudyInstanceUid),
            patientId: request.PatientId,
            patientName: request.PatientName,
            sourceNodeId: request.NodeId,
            accessionNumber: request.AccessionNumber,
            studyDate: request.StudyDate,
            studyDescription: request.StudyDescription,
            patientRecordId: newStudyPatientRecordId);

        study.RecordImagesReceived(request.InstanceCount, request.TotalSizeBytes, seriesCount: request.SeriesCount);

        await studyRepository.AddAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogDebug(
            "Study created via progress from node {NodeId}: StudyUID={StudyUid}",
            request.NodeId, request.StudyInstanceUid);

        return new EdgeStudyNotifyResult(true, study.Id, DateTime.UtcNow);
    }

    public async Task<EdgeStudyNotifyResult?> ProcessStudyPacsStatusAsync(
        StudyPacsStatusNotifyRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        var study = await studyRepository.GetByStudyInstanceUidAsync(request.StudyInstanceUid, ct);
        if (study is null)
        {
            logger.LogWarning(
                "PACS status report for unknown study {StudyUid} from node {NodeId} — ignored",
                request.StudyInstanceUid, request.NodeId);
            return null;
        }

        switch (request.Status)
        {
            case nameof(StudyStatus.Sending):
                study.MarkSendingToPacs();
                break;
            case nameof(StudyStatus.SentToPacs):
                study.MarkSentToPacs();
                break;
            case nameof(StudyStatus.Failed):
                study.MarkFailed(request.Error ?? "PACS send failed");
                break;
            default:
                logger.LogWarning(
                    "Unsupported PACS status '{Status}' for study {StudyUid} from node {NodeId} — ignored",
                    request.Status, request.StudyInstanceUid, request.NodeId);
                return null;
        }

        await studyRepository.UpdateAsync(study, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Study {StudyUid} PACS status advanced to {Status} (node {NodeId}, PACS {Pacs})",
            request.StudyInstanceUid, request.Status, request.NodeId, request.TargetPacsAeTitle);

        await NotifyStatusChangedAsync(study, request.NodeId, ct);
        return new EdgeStudyNotifyResult(true, study.Id, DateTime.UtcNow);
    }

    public async Task<EdgeOperationResult?> ProcessHealthReportAsync(
        NodeHealthReportRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null) return null;

        // Un reporte de salud también es prueba de vida.
        node.UpdateHeartbeat();

        node.UpdateStorage(
            dicomMb: request.StorageDicomMb,
            databaseMb: request.StorageDatabaseMb,
            volumeFreeMb: request.StorageVolumeFreeMb,
            volumeTotalMb: request.StorageVolumeTotalMb,
            limitAppliedMb: request.StorageLimitMb,
            measuredAt: request.StorageMeasuredAt);

        node.ConfirmConfigVersion(request.AppliedConfigVersion);

        await nodeRepository.UpdateAsync(node, ct);

        var record = HealthCheckRecord.Create(
            request.NodeId,
            NodeStatus.Online,
            cpuUsagePercent: request.CpuPercent,
            storageDicomMb: request.StorageDicomMb,
            storageDatabaseMb: request.StorageDatabaseMb,
            storageVolumeFreeMb: request.StorageVolumeFreeMb,
            storageLimitMb: request.StorageLimitMb,
            memoryUsageMb: request.MemoryPercent is not null ? (long)request.MemoryPercent : null,
            queuedStudies: request.QueueDepth);

        await healthCheckRepository.AddAsync(record, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Health report persisted from node {NodeId}: DICOM={DicomMb}MB, DB={DbMb}MB, " +
            "limit={LimitMb}MB, volume free={FreeMb}MB, CPU={Cpu}%, Mem={Mem}%",
            request.NodeId, request.StorageDicomMb, request.StorageDatabaseMb,
            request.StorageLimitMb, request.StorageVolumeFreeMb,
            request.CpuPercent, request.MemoryPercent);

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
        // Store optimistically without checking node existence — the echo status
        // lives in an in-memory store, so a stale node id is only a cosmetic issue.
        // (Do NOT fire-and-forget an EF query here: an un-awaited DbContext read
        // outlives the request scope, disposing the context mid-read and corrupting
        // the Npgsql connection — "BindComplete while expecting ReadyForQueryMessage".)
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

    public async Task<EdgeOperationResult?> ProcessEquipmentStatusReportAsync(
        NodeEquipmentStatusReportRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(request.NodeId, ct);
        if (node is null)
            return null;

        var updated = 0;
        foreach (var entry in request.Equipment)
        {
            var equipment = await equipmentRepository.GetByNodeAndAeTitleAsync(request.NodeId, entry.AeTitle, ct);
            if (equipment is null)
                continue; // Unknown AE (cosmetic) — skip.

            // Only advance LastConnectionAt — avoids needless writes on repeated reports.
            if (equipment.LastConnectionAt is null || entry.LastSeenUtc > equipment.LastConnectionAt)
            {
                equipment.MarkConnected(entry.LastSeenUtc);
                await equipmentRepository.UpdateAsync(equipment, ct);
                updated++;
            }
        }

        if (updated > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogDebug(
            "Equipment presence report for node {NodeId}: {Updated}/{Total} equipment updated",
            request.NodeId, updated, request.Equipment.Count);

        return new EdgeOperationResult(true, DateTime.UtcNow);
    }
}
