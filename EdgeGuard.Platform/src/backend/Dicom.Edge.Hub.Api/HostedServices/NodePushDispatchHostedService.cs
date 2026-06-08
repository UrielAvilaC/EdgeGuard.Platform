using Dicom.Edge.Hub.Api.Hubs;
using Dicom.Edge.Hub.Application.Equipment;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Application.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Api.HostedServices;

/// <summary>
/// Consumes <see cref="INodePushQueue"/> and performs configuration pushes to Edge
/// Nodes off the HTTP request path. Each result is broadcast over SignalR
/// (<c>NodePushStatus</c>) so the SPA can reflect status without blocking on the
/// node round-trip.
/// </summary>
/// <remarks>
/// Lives in the API project because it needs <see cref="IHubContext{THub}"/>, which
/// is only available where the SignalR hub is hosted. Failures are logged and
/// reported via SignalR — a single failed/slow node never blocks others or the host.
/// </remarks>
public sealed class NodePushDispatchHostedService(
    INodePushQueue queue,
    IServiceScopeFactory scopeFactory,
    IHubContext<EdgeHubNotificationHub> hub,
    ILogger<NodePushDispatchHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Node push dispatch worker started");

        try
        {
            await foreach (var request in queue.DequeueAllAsync(stoppingToken))
                await DispatchAsync(request, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }

        logger.LogInformation("Node push dispatch worker stopped");
    }

    private async Task DispatchAsync(NodePushRequest request, CancellationToken ct)
    {
        bool success;
        string? error = null;

        try
        {
            using var scope = scopeFactory.CreateScope();
            success = await PushAsync(scope.ServiceProvider, request, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Shutting down — stop the loop.
        }
        catch (Exception ex)
        {
            success = false;
            error = ex.Message;
            logger.LogError(ex, "Push dispatch failed for node {NodeId} kind {Kind}",
                request.NodeId, request.Kind);
        }

        await NotifyAsync(request, success, error);
    }

    private static async Task<bool> PushAsync(
        IServiceProvider sp, NodePushRequest request, CancellationToken ct) =>
        request.Kind switch
        {
            NodePushKind.Config =>
                (await sp.GetRequiredService<INodeConfigPushService>()
                         .PushConfigAsync(request.NodeId, ct)).Success,
            NodePushKind.Rules =>
                await sp.GetRequiredService<INodeDicomRoutingRulePushService>()
                        .PushAsync(request.NodeId, ct),
            NodePushKind.Pacs =>
                await sp.GetRequiredService<INodePacsDestinationPushService>()
                        .PushAsync(request.NodeId, ct),
            NodePushKind.Equipment =>
                await sp.GetRequiredService<INodeEquipmentPushService>()
                        .PushAsync(request.NodeId, ct),
            _ => false
        };

    private async Task NotifyAsync(NodePushRequest request, bool success, string? error)
    {
        try
        {
            await hub.NotifyNodePushStatus(request.NodeId, new
            {
                nodeId = request.NodeId,
                kind = request.Kind.ToString(),
                success,
                error
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast NodePushStatus for node {NodeId}", request.NodeId);
        }
    }
}
