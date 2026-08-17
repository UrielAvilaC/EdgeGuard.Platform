using Dicom.Edge.Hub.Api.Hubs;
using Dicom.Edge.Hub.Application.Studies;
using Microsoft.AspNetCore.SignalR;

namespace Dicom.Edge.Hub.Api.HostedServices;

/// <summary>
/// SignalR transport for <see cref="IStudyRealtimeNotifier"/>: broadcasts each study status
/// transition to the "dashboard" group via <see cref="EdgeHubNotificationHub"/> so the SPA
/// refreshes the dashboard and study list/detail without polling.
/// </summary>
public sealed class StudySignalRNotifier(IHubContext<EdgeHubNotificationHub> hub) : IStudyRealtimeNotifier
{
    public Task StatusChangedAsync(StudyStatusChange change, CancellationToken ct = default) =>
        hub.NotifyStudyStatusChanged(new
        {
            studyId = change.StudyId,
            status = change.Status,
            patientName = change.PatientName,
            nodeId = change.NodeId,
            timestamp = change.TimestampUtc,
        });
}
