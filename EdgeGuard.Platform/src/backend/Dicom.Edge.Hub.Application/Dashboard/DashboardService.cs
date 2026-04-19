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
        var studyCountTask = studyRepository.CountAsync(ct);
        var patientCountTask = patientRepository.CountAsync(ct);
        var nodeCountTask = nodeRepository.CountAsync(ct);
        var nodesTask = nodeRepository.GetAllAsync(ct);
        var pendingTask = studyRepository.GetPendingForPacsAsync(ct);
        var failedTask = studyRepository.GetByStatusAsync(StudyStatus.Failed, ct);
        var recentTask = studyRepository.GetPagedAsync(new Common.Pagination.PaginationRequest { Page = 1, PageSize = 10 }, ct);
        var queueTask = hl7MonitoringService.GetQueueSummaryAsync(ct);

        await Task.WhenAll(studyCountTask, patientCountTask, nodeCountTask, nodesTask,
            pendingTask, failedTask, recentTask, queueTask);

        var nodes = await nodesTask;
        var activeNodes = nodes.Count(n => n.IsEnabled && n.Status != NodeStatus.Offline);

        Hl7ListenerStatusDto? hl7Status = null;
        try { hl7Status = hl7MonitoringService.GetListenerStatus(); } catch { /* optional */ }

        return new DashboardSummaryDto
        {
            TotalStudies = await studyCountTask,
            TotalPatients = await patientCountTask,
            TotalNodes = await nodeCountTask,
            ActiveNodes = activeNodes,
            PendingPacsStudies = (await pendingTask).Count,
            FailedStudies = (await failedTask).Count,
            QueueSummary = await queueTask,
            Hl7Status = hl7Status,
            RecentStudies = (await recentTask).Items.Select(s => new StudyDto
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
                SeriesCount = s.Series.Count,
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
                MaxStorageMb = n.MaxStorageMb,
                AvailableStorageMb = n.AvailableStorageMb,
                TotalStudiesReceived = n.TotalStudiesReceived,
                TotalStudiesSent = n.TotalStudiesSent,
                ErrorsLast24Hours = n.ErrorsLast24Hours,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList()
        };
    }
}
