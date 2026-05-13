using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Propagates global PACS assignments to nodes.
/// </summary>
public class PacsInheritanceService : IPacsInheritanceService
{
    private readonly INodeRepository _nodeRepository;
    private readonly IPacsServerRepository _pacsRepository;
    private readonly ILogger<PacsInheritanceService> _logger;

    public PacsInheritanceService(
        INodeRepository nodeRepository,
        IPacsServerRepository pacsRepository,
        ILogger<PacsInheritanceService> logger)
    {
        _nodeRepository = nodeRepository;
        _pacsRepository = pacsRepository;
        _logger = logger;
    }

    public async Task InheritGlobalPacsToNodeAsync(string nodeId, CancellationToken ct = default)
    {
        var node = await _nodeRepository.GetWithPacsAssignmentsAsync(nodeId, ct);
        if (node is null)
        {
            _logger.LogWarning("Node {NodeId} not found for PACS inheritance", nodeId);
            return;
        }

        var globalPacs = await _pacsRepository.GetGlobalAsync(ct);

        foreach (var pacs in globalPacs)
        {
            node.AssignPacs(pacs.Id, inheritedFromHub: true);
            _logger.LogInformation(
                "Inherited global PACS {PacsId} ({AeTitle}) to node {NodeId}",
                pacs.Id, pacs.AeTitle.Value, nodeId);
        }

        await _nodeRepository.UpdateAsync(node, ct);
    }

    public async Task PropagateGlobalPacsToAllNodesAsync(string pacsId, CancellationToken ct = default)
    {
        var pacs = await _pacsRepository.GetByIdAsync(pacsId, ct);
        if (pacs is null || !pacs.IsGlobal)
        {
            _logger.LogWarning("PACS {PacsId} not found or not global", pacsId);
            return;
        }

        var activeNodes = await _nodeRepository.GetActiveNodesAsync(ct);

        foreach (var node in activeNodes)
        {
            var nodeWithAssignments = await _nodeRepository.GetWithPacsAssignmentsAsync(node.Id, ct);
            if (nodeWithAssignments is null) continue;

            nodeWithAssignments.AssignPacs(pacsId, inheritedFromHub: true);
            await _nodeRepository.UpdateAsync(nodeWithAssignments, ct);

            _logger.LogInformation(
                "Propagated global PACS {PacsId} to node {NodeId}",
                pacsId, node.Id);
        }
    }
}
