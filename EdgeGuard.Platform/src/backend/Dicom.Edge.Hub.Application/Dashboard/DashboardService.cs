using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.Hl7;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}

public sealed class DashboardService(
    IStudyRepository studyRepository,
    IPatientRepository patientRepository,
    INodeRepository nodeRepository,
    IHl7MonitoringService hl7MonitoringService) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        // Await each DbContext call sequentially to avoid concurrent access on the same DbContext instance.
        var studyCount = await studyRepository.CountAsync(ct);
        var patientCount = await patientRepository.CountAsync(ct);
        var nodeCount = await nodeRepository.CountAsync(ct);
        var nodes = await nodeRepository.GetAllAsync(ct);
        var pending = await studyRepository.GetPendingForPacsAsync(ct);
        var failed = await studyRepository.GetByPacsStatusAsync(StudyPacsStatus.Failed, ct);
        var recent = await studyRepository.GetPagedAsync(new Common.Pagination.PaginationRequest { Page = 1, PageSize = 10 }, ct);
        var queueSummary = await hl7MonitoringService.GetQueueSummaryAsync(ct);

        var activeNodes = nodes.Count(n => n.IsEnabled && n.Status != NodeStatus.Offline);

        Hl7ListenerStatusDto? hl7Status = null;
        try { hl7Status = hl7MonitoringService.GetListenerStatus(); } catch { /* optional */ }

        return new DashboardSummaryDto
        {
            TotalStudies = studyCount,
            TotalPatients = patientCount,
            TotalNodes = nodeCount,
            ActiveNodes = activeNodes,
            PendingPacsStudies = pending.Count,
            FailedStudies = failed.Count,
            QueueSummary = queueSummary,
            Hl7Status = hl7Status,
            RecentStudies = recent.Items.Select(s => new StudyDto
            {
                Id = s.Id,
                StudyInstanceUid = s.StudyInstanceUid.Value,
                AccessionNumber = s.AccessionNumber,
                StudyDate = s.StudyDate,
                StudyDescription = s.StudyDescription,
                ReferringPhysician = s.ReferringPhysician,
                PatientId = s.PatientId,
                PatientName = s.PatientName,
                SourceNodeId = s.SourceNodeId,
                SourceAeTitle = s.SourceAeTitle,
                Status = s.Status.ToString(),
                InstanceCount = s.InstanceCount,
                SeriesCount = s.SeriesCount,
                TotalSizeBytes = s.TotalSizeBytes,
                Priority = s.Priority,
                IsUrgent = s.IsUrgent,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList(),
            Nodes = nodes.Select(n => new NodeDto
            {
                Id = n.Id,
                Name = n.Name,
                AeTitle = n.AeTitle.Value,
                IpAddress = n.IpAddress,
                Port = n.Port,
                Status = n.Status.ToString(),
                IsEnabled = n.IsEnabled,
                LastHeartbeatAt = n.LastHeartbeatAt,
                HealthCheckIntervalSeconds = n.HealthCheckIntervalSeconds,
                StorageLimitMb = n.StorageLimitMb,
                StorageDicomMb = n.StorageDicomMb,
                StorageDatabaseMb = n.StorageDatabaseMb,
                StorageMeasuredAt = n.StorageMeasuredAt,
                TotalStudiesReceived = n.TotalStudiesReceived,
                TotalStudiesSent = n.TotalStudiesSent,
                ErrorsLast24Hours = n.ErrorsLast24Hours,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList()
        };
    }
}
