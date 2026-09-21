using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Application.PacsServers;

/// <summary>
/// Application service for PacsServer write operations.
/// After any PACS change, pushes updated PACS destinations to all active nodes
/// so the node routing cache stays consistent without waiting for a config pull.
/// </summary>
public sealed class PacsServerService(
    IPacsServerRepository pacsRepository,
    INodeRepository nodeRepository,
    INodeOutbox nodeOutbox,
    IOptions<NodePushOptions> pushOptions,
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

        // Las asignaciones nodo→PACS no son clave foránea, así que nadie las retira solo.
        // Con el borrado físico anterior quedaban apuntando a una fila inexistente; ahora
        // apuntarían a una invisible, que en la práctica es igual de roto. Se desasigna
        // explícitamente antes de retirar el PACS.
        var desasignados = await UnassignFromAllNodesAsync(id, ct);

        await pacsRepository.DeleteAsync(id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "PACS server soft-deleted: {PacsId} (unassigned from {Count} node(s))", id, desasignados);
        _ = PushPacsToAllNodesAsync();
        return true;
    }

    /// <summary>
    /// Retira el PACS de todos los nodos que lo tuvieran asignado. Devuelve cuántos.
    /// </summary>
    private async Task<int> UnassignFromAllNodesAsync(string pacsId, CancellationToken ct)
    {
        var nodos = await nodeRepository.GetAllForUpdateAsync(ct);
        var afectados = 0;

        foreach (var nodo in nodos)
        {
            var conAsignaciones = await nodeRepository.GetWithPacsAssignmentsAsync(nodo.Id, ct);
            if (conAsignaciones is null) continue;
            if (!conAsignaciones.PacsAssignments.Any(a => a.PacsId == pacsId && a.IsActive)) continue;

            conAsignaciones.UnassignPacs(pacsId);
            await nodeRepository.UpdateAsync(conAsignaciones, ct);
            afectados++;
        }

        return afectados;
    }

    /// <summary>
    /// Fans out updated PACS destinations to all active nodes after a PACS change.
    /// P1-1: when async push is enabled (default), each node is enqueued and the
    /// background dispatcher performs the HTTP pushes in parallel off the request
    /// path (reporting status via SignalR). Otherwise falls back to the legacy
    /// sequential in-scope push. The node lookup itself is a single fast query.
    /// </summary>
    private async Task PushPacsToAllNodesAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var nodeRepo = scope.ServiceProvider.GetRequiredService<INodeRepository>();

            var nodes = await nodeRepo.GetActiveNodesAsync(CancellationToken.None);
            var targets = nodes.Where(n => !string.IsNullOrWhiteSpace(n.ApiEndpoint));

            if (pushOptions.Value.Async)
            {
                foreach (var node in targets)
                    await nodeOutbox.EnqueueAsync(node.Id, NodePushKind.Pacs, CancellationToken.None);
                return;
            }

            var pushSvc = scope.ServiceProvider.GetRequiredService<INodePacsDestinationPushService>();
            foreach (var node in targets)
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
