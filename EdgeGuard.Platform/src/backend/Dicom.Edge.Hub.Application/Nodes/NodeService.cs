using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Nodes;

/// <summary>
/// Application service for Node write operations.
/// Delegates entity creation to the domain factory and manages persistence via UoW.
/// </summary>
public sealed class NodeService(
    INodeRepository nodeRepository,
    IPacsServerRepository pacsRepository,
    INodeConfigPushService configPushService,
    INodePacsDestinationPushService pacsDestinationPushService,
    IServiceScopeFactory scopeFactory,
    IUnitOfWork unitOfWork,
    ILogger<NodeService> logger) : INodeService
{
    public async Task<Node> CreateAsync(CreateNodeRequest request, CancellationToken ct = default)
    {
        var node = Node.Create(
            request.Name,
            request.IpAddress,
            request.Port,
            request.ApiEndpoint,
            request.Location,
            request.FacilityName,
            request.HealthCheckIntervalSeconds);

        await nodeRepository.AddAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Node created: {NodeId} {Name}", node.Id, node.Name);
        return node;
    }

    public async Task<bool> UpdateAsync(string id, UpdateNodeRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(id, ct);
        if (node is null) return false;

        node.UpdateConfiguration(
            request.Location,
            request.FacilityName,
            request.TimeZone,
            version: null,
            request.HealthCheckIntervalSeconds,
            request.MaxStorageMb);

        await nodeRepository.UpdateAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Node updated: {NodeId}", id);
        return true;
    }

    public async Task<bool> EnableAsync(string id, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(id, ct);
        if (node is null) return false;

        node.Enable();
        await nodeRepository.UpdateAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Node enabled: {NodeId}", id);
        return true;
    }

    public async Task<bool> DisableAsync(string id, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetByIdAsync(id, ct);
        if (node is null) return false;

        node.Disable();
        await nodeRepository.UpdateAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Node disabled: {NodeId}", id);
        return true;
    }

    public async Task<bool> AssignPacsAsync(
        string nodeId, string pacsId, AssignPacsRequest request, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetWithPacsAssignmentsAsync(nodeId, ct);
        if (node is null) return false;

        var pacs = await pacsRepository.GetByIdAsync(pacsId, ct);
        if (pacs is null) return false;

        node.AssignPacs(pacsId, inheritedFromHub: false, request.CEchoIntervalSeconds);
        await nodeRepository.UpdateAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS {PacsId} assigned to node {NodeId}", pacsId, nodeId);

        // Push config (settings) + PACS destinations separately and immediately
        if (!string.IsNullOrWhiteSpace(node.ApiEndpoint))
        {
            _ = PushConfigInNewScopeAsync(nodeId);
            _ = PushPacsInNewScopeAsync(nodeId);
        }

        return true;
    }

    public async Task<bool> UnassignPacsAsync(
        string nodeId, string pacsId, CancellationToken ct = default)
    {
        var node = await nodeRepository.GetWithPacsAssignmentsAsync(nodeId, ct);
        if (node is null) return false;

        if (!node.PacsAssignments.Any(a => a.PacsId == pacsId && a.IsActive))
            return false;

        node.UnassignPacs(pacsId);
        await nodeRepository.UpdateAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS {PacsId} unassigned from node {NodeId}", pacsId, nodeId);

        // Push config (settings) + PACS destinations separately and immediately
        if (!string.IsNullOrWhiteSpace(node.ApiEndpoint))
        {
            _ = PushConfigInNewScopeAsync(nodeId);
            _ = PushPacsInNewScopeAsync(nodeId);
        }

        return true;
    }

    /// <summary>
    /// Runs the config push in a brand-new DI scope so it gets its own DbContext.
    /// This avoids 'The reader is closed' when the request scope is disposed while
    /// the fire-and-forget task is still executing EF queries.
    /// Uses <see cref="CancellationToken.None"/> so the push is never cancelled
    /// by the HTTP request's CancellationToken.
    /// </summary>
    private async Task PushConfigInNewScopeAsync(string nodeId)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var push = scope.ServiceProvider.GetRequiredService<INodeConfigPushService>();
            var result = await push.PushConfigAsync(nodeId, CancellationToken.None);

            if (!result.Success)
                logger.LogWarning(
                    "Config push to node {NodeId} failed: {Error} — node will sync on next pull cycle",
                    nodeId, result.Error);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Config push to node {NodeId} threw an exception — node will sync on next pull cycle",
                nodeId);
        }
    }

    private async Task PushPacsInNewScopeAsync(string nodeId)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var push = scope.ServiceProvider.GetRequiredService<INodePacsDestinationPushService>();
            var ok = await push.PushAsync(nodeId, CancellationToken.None);

            if (!ok)
                logger.LogWarning(
                    "PACS destinations push to node {NodeId} failed — node will sync on next config pull",
                    nodeId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "PACS destinations push to node {NodeId} threw — node will sync on next config pull",
                nodeId);
        }
    }
}
