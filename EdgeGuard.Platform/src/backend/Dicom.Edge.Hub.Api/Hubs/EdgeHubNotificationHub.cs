using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Dicom.Edge.Hub.Api.Hubs;

/// <summary>
/// SignalR hub for real-time dashboard and event notifications.
/// Clients join groups to receive targeted updates.
/// </summary>
[Authorize]
public sealed class EdgeHubNotificationHub : Microsoft.AspNetCore.SignalR.Hub
{
    /// <summary>
    /// Joins the "dashboard" group for receiving KPI/summary updates.
    /// </summary>
    public Task JoinDashboard() =>
        Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");

    /// <summary>
    /// Leaves the "dashboard" group.
    /// </summary>
    public Task LeaveDashboard() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");

    /// <summary>
    /// Joins a node-specific group for receiving node events.
    /// </summary>
    public Task JoinNode(string nodeId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"node-{nodeId}");

    /// <summary>
    /// Leaves a node-specific group.
    /// </summary>
    public Task LeaveNode(string nodeId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"node-{nodeId}");

    /// <summary>Joins the "outbox" group for live outbox activity updates.</summary>
    public Task JoinOutbox() =>
        Groups.AddToGroupAsync(Context.ConnectionId, "outbox");

    /// <summary>Leaves the "outbox" group.</summary>
    public Task LeaveOutbox() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, "outbox");
}

/// <summary>
/// Extension methods for broadcasting real-time events via SignalR.
/// Inject <see cref="IHubContext{EdgeHubNotificationHub}"/> and use these helpers.
/// </summary>
public static class HubNotificationExtensions
{
    public static Task NotifyStudyReceived(this IHubContext<EdgeHubNotificationHub> hub, object payload) =>
        hub.Clients.Group("dashboard").SendAsync("StudyReceived", payload);

    public static Task NotifyStudyStatusChanged(this IHubContext<EdgeHubNotificationHub> hub, object payload) =>
        hub.Clients.Group("dashboard").SendAsync("StudyStatusChanged", payload);

    public static Task NotifyNodeStatusChanged(this IHubContext<EdgeHubNotificationHub> hub, string nodeId, object payload) =>
        Task.WhenAll(
            hub.Clients.Group("dashboard").SendAsync("NodeStatusChanged", payload),
            hub.Clients.Group($"node-{nodeId}").SendAsync("NodeStatusChanged", payload));

    /// <summary>
    /// Broadcasts the result of an asynchronous configuration push to a node so the
    /// SPA can show success/failure without blocking on the HTTP request. Payload:
    /// <c>{ nodeId, kind, success, error? }</c>.
    /// </summary>
    public static Task NotifyNodePushStatus(this IHubContext<EdgeHubNotificationHub> hub, string nodeId, object payload) =>
        Task.WhenAll(
            hub.Clients.Group("dashboard").SendAsync("NodePushStatus", payload),
            hub.Clients.Group($"node-{nodeId}").SendAsync("NodePushStatus", payload));

    /// <summary>
    /// Broadcasts a change to one outbox entry (node-sync or notification) to the "outbox"
    /// group so the monitoring UI updates live. Payload:
    /// <c>{ category, id, topicId, status, attempts, error? }</c>.
    /// </summary>
    public static Task NotifyOutboxEntryChanged(this IHubContext<EdgeHubNotificationHub> hub, object payload) =>
        hub.Clients.Group("outbox").SendAsync("OutboxEntryChanged", payload);

    public static Task NotifyNodeHeartbeat(this IHubContext<EdgeHubNotificationHub> hub, string nodeId, object payload) =>
        hub.Clients.Group($"node-{nodeId}").SendAsync("NodeHeartbeat", payload);

    public static Task NotifyHl7MessageReceived(this IHubContext<EdgeHubNotificationHub> hub, object payload) =>
        hub.Clients.Group("dashboard").SendAsync("Hl7MessageReceived", payload);

    public static Task NotifyWhatsAppSent(this IHubContext<EdgeHubNotificationHub> hub, object payload) =>
        hub.Clients.Group("dashboard").SendAsync("WhatsAppNotificationSent", payload);

    public static Task NotifyAuditEvent(this IHubContext<EdgeHubNotificationHub> hub, object payload) =>
        hub.Clients.Group("dashboard").SendAsync("AuditEvent", payload);
}
