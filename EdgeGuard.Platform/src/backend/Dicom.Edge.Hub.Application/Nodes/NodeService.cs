using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Nodes;

/// <summary>
/// Application service for Node write operations.
/// Delegates entity creation to the domain factory and manages persistence via UoW.
/// </summary>
public sealed class NodeService(
    INodeRepository nodeRepository,
    IUnitOfWork unitOfWork,
    ILogger<NodeService> logger) : INodeService
{
    public async Task<Node> CreateAsync(CreateNodeRequest request, CancellationToken ct = default)
    {
        var node = Node.Create(
            request.Name,
            AeTitle.Create(request.AeTitle),
            request.IpAddress,
            request.Port,
            request.ApiEndpoint,
            request.Location,
            request.FacilityName,
            request.HealthCheckIntervalSeconds);

        await nodeRepository.AddAsync(node, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Node created: {NodeId} {AeTitle}", node.Id, request.AeTitle);
        return node;
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
}
