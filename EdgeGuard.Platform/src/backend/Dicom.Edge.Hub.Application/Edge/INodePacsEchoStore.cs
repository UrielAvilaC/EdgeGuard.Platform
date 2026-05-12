using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Hub.Application.Edge;

/// <summary>
/// In-memory store for the latest PACS C-ECHO results per node.
/// Updated each time a node reports its C-ECHO status to the Hub.
/// </summary>
public interface INodePacsEchoStore
{
    /// <summary>Stores (or replaces) the latest C-ECHO status for a node.</summary>
    void Upsert(NodePacsCEchoStatusDto status);

    /// <summary>Returns the latest C-ECHO status for the given node, or null if never reported.</summary>
    NodePacsCEchoStatusDto? Get(string nodeId);
}
