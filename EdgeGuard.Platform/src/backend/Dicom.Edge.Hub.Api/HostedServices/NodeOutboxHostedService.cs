using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Api.Hubs;
using Dicom.Edge.Hub.Application.Equipment;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Application.Outbox;
using Dicom.Edge.Hub.Application.Routing;
using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Api.HostedServices;

/// <summary>
/// Drains the durable <c>node_outbox_messages</c> store and performs configuration pushes to
/// Edge Nodes off the HTTP request path, with at-least-once delivery, exponential backoff and
/// dead-lettering (shared loop in <see cref="OutboxDispatcherBase"/>). Each result is broadcast
/// over SignalR (<c>NodePushStatus</c>). Replaces the in-memory <c>NodePushDispatchHostedService</c>.
/// Lives in the API project because it needs <see cref="IHubContext{THub}"/>.
/// </summary>
public sealed class NodeOutboxHostedService(
    IServiceScopeFactory scopeFactory,
    IHubContext<EdgeHubNotificationHub> hub,
    IOutboxNotifier notifier,
    ILogger<NodeOutboxHostedService> logger)
    : OutboxDispatcherBase(scopeFactory, logger)
{
    private const int BatchSize = 100;
    private const int MaxAttempts = 5;

    protected override string WorkerName => "Node outbox worker";
    protected override int IntervalSeconds => 10;

    protected override async Task DrainAsync(IServiceScope scope, CancellationToken ct)
    {
        var repo = scope.ServiceProvider.GetRequiredService<INodeOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var due = await repo.GetDuePendingAsync(BatchSize, ct);
        if (due.Count == 0) return;

        // Per-node lanes: preserve enqueue order within a node and isolate a slow/failing
        // node from the others.
        var lanes = due
            .GroupBy(m => m.NodeId)
            .Select(group => ProcessNodeAsync([.. group], scope.ServiceProvider, repo, ct));
        await Task.WhenAll(lanes);

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ProcessNodeAsync(
        List<NodeOutboxMessage> items,
        IServiceProvider sp,
        INodeOutboxRepository repo,
        CancellationToken ct)
    {
        foreach (var m in items.OrderBy(x => x.CreatedAt))
        {
            bool success;
            string? error = null;
            try
            {
                success = await PushAsync(sp, m.TopicId, m.NodeId, ct);
                if (!success) error = "Push returned failure";
            }
            catch (Exception ex)
            {
                success = false;
                error = ex.Message;
                logger.LogError(ex, "Node outbox push {Topic} failed for node {NodeId}", m.TopicId, m.NodeId);
            }

            if (success)
                m.MarkSent();
            else if (m.Attempts + 1 >= MaxAttempts)
                m.MarkFailed(error ?? "Push failed");
            else
                m.ScheduleRetry(Backoff(m.Attempts), error ?? "Push failed");

            await repo.UpdateAsync(m, ct);
            await NotifyAsync(m.NodeId, m.TopicId, success, success ? null : error);
            await notifier.EntryChangedAsync(new OutboxEntryChange(
                OutboxTopicCatalog.Categories.NodeSync, m.Id, m.TopicId,
                m.Status.ToString(), m.Attempts, m.LastError), ct);
        }
    }

    private static Task<bool> PushAsync(IServiceProvider sp, string topicId, string nodeId, CancellationToken ct) =>
        topicId switch
        {
            OutboxTopicCatalog.NodeConfig => ConfigPushAsync(sp, nodeId, ct),
            OutboxTopicCatalog.NodeRules =>
                sp.GetRequiredService<INodeDicomRoutingRulePushService>().PushAsync(nodeId, ct),
            OutboxTopicCatalog.NodePacs =>
                sp.GetRequiredService<INodePacsDestinationPushService>().PushAsync(nodeId, ct),
            OutboxTopicCatalog.NodeEquipment =>
                sp.GetRequiredService<INodeEquipmentPushService>().PushAsync(nodeId, ct),
            _ => Task.FromResult(false)
        };

    private static async Task<bool> ConfigPushAsync(IServiceProvider sp, string nodeId, CancellationToken ct) =>
        (await sp.GetRequiredService<INodeConfigPushService>().PushConfigAsync(nodeId, ct)).Success;

    private async Task NotifyAsync(string nodeId, string topicId, bool success, string? error)
    {
        try
        {
            await hub.NotifyNodePushStatus(nodeId, new
            {
                nodeId,
                kind = topicId,
                success,
                error
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast NodePushStatus for node {NodeId}", nodeId);
        }
    }
}
