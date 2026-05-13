using System.Collections.Concurrent;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.Edge;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Thread-safe singleton in-memory store for the latest PACS C-ECHO status per node.
/// No persistence needed — the node re-reports every C-ECHO interval, so data
/// is naturally refreshed on startup.
/// </summary>
public sealed class NodePacsEchoStore : INodePacsEchoStore
{
    private readonly ConcurrentDictionary<string, NodePacsCEchoStatusDto> _store =
        new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(NodePacsCEchoStatusDto status) =>
        _store[status.NodeId] = status;

    public NodePacsCEchoStatusDto? Get(string nodeId) =>
        _store.TryGetValue(nodeId, out var status) ? status : null;
}
