using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.PacsServers;

/// <summary>
/// Application service for PacsServer write operations.
/// After any PACS change, pushes updated PACS destinations to all active nodes
/// so the node routing cache stays consistent without waiting for a config pull.
/// </summary>
public sealed class PacsServerService(
    IPacsServerRepository pacsRepository,
    INodeRepository nodeRepository,
    INodePacsDestinationPushService pacsDestinationPushService,
    IServiceScopeFactory scopeFactory,
    IUnitOfWork unitOfWork,
    ILogger<PacsServerService> logger) : IPacsServerService
{
    public async Task<PacsServer> CreateAsync(CreatePacsServerRequest request, CancellationToken ct = default)
    {
        var pacs = PacsServer.Create(
            request.Name,
            AeTitle.Create(request.AeTitle),
            request.HostName,
            request.Port,
            request.Description,
            request.IsGlobal,
            request.MaxConcurrentAssociations,
            request.TimeoutSeconds);

        await pacsRepository.AddAsync(pacs, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server created: {PacsId} {AeTitle}", pacs.Id, request.AeTitle);
        return pacs;
    }

    public async Task<bool> UpdateAsync(string id, UpdatePacsServerRequest request, CancellationToken ct = default)
    {
        var pacs = await pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return false;

        pacs.UpdateConfiguration(
            request.Name,
            request.HostName,
            request.Port,
            request.Description,
            request.MaxConcurrentAssociations,
            request.TimeoutSeconds);

        await pacsRepository.UpdateAsync(pacs, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server updated: {PacsId}", id);
        _ = PushPacsToAllNodesAsync();
        return true;
    }

    public async Task<bool> EnableAsync(string id, CancellationToken ct = default)
    {
        var pacs = await pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return false;

        pacs.Enable();
        await pacsRepository.UpdateAsync(pacs, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server enabled: {PacsId}", id);
        _ = PushPacsToAllNodesAsync();
        return true;
    }

    public async Task<bool> DisableAsync(string id, CancellationToken ct = default)
    {
        var pacs = await pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return false;

        pacs.Disable();
        await pacsRepository.UpdateAsync(pacs, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server disabled: {PacsId}", id);
        _ = PushPacsToAllNodesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var pacs = await pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return false;

        await pacsRepository.DeleteAsync(id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server deleted: {PacsId}", id);
        _ = PushPacsToAllNodesAsync();
        return true;
    }

    /// <summary>
    /// Fire-and-forget: pushes updated PACS destinations to all active nodes in a new DI scope.
    /// Uses CancellationToken.None so the push is not cancelled by the originating HTTP request.
    /// </summary>
    private async Task PushPacsToAllNodesAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var nodeRepo = scope.ServiceProvider.GetRequiredService<INodeRepository>();
            var pushSvc  = scope.ServiceProvider.GetRequiredService<INodePacsDestinationPushService>();

            var nodes = await nodeRepo.GetActiveNodesAsync(CancellationToken.None);
            foreach (var node in nodes.Where(n => !string.IsNullOrWhiteSpace(n.ApiEndpoint)))
            {
                try
                {
                    await pushSvc.PushAsync(node.Id, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "PACS push failed for node {NodeId}", node.Id);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PushPacsToAllNodesAsync failed");
        }
    }
}
