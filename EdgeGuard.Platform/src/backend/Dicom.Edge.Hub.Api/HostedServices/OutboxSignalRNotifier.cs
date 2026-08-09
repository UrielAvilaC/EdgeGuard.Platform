using Dicom.Edge.Hub.Api.Hubs;
using Dicom.Edge.Hub.Application.Outbox;
using Microsoft.AspNetCore.SignalR;

namespace Dicom.Edge.Hub.Api.HostedServices;

/// <summary>
/// SignalR transport for <see cref="IOutboxNotifier"/>: broadcasts each change to the
/// "outbox" group via <see cref="EdgeHubNotificationHub"/>.
/// </summary>
public sealed class OutboxSignalRNotifier(IHubContext<EdgeHubNotificationHub> hub) : IOutboxNotifier
{
    public Task EntryChangedAsync(OutboxEntryChange change, CancellationToken ct = default) =>
        hub.NotifyOutboxEntryChanged(new
        {
            category = change.Category,
            id = change.Id,
            topicId = change.TopicId,
            status = change.Status,
            attempts = change.Attempts,
            error = change.Error
        });
}
