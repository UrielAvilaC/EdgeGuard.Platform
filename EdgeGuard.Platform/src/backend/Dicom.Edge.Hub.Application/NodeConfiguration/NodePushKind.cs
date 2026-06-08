namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Kind of configuration push to dispatch to an Edge Node. Maps to the
/// corresponding push service in the dispatcher.
/// </summary>
public enum NodePushKind
{
    /// <summary>Full configuration snapshot (<c>INodeConfigPushService</c>).</summary>
    Config,

    /// <summary>DICOM routing rules (<c>INodeDicomRoutingRulePushService</c>).</summary>
    Rules,

    /// <summary>PACS destination assignments (<c>INodePacsDestinationPushService</c>).</summary>
    Pacs,

    /// <summary>Equipment catalog with modality codes (<c>INodeEquipmentPushService</c>).</summary>
    Equipment
}

/// <summary>
/// A queued request to push configuration of a given <see cref="NodePushKind"/>
/// to a single Edge Node, off the originating HTTP request path.
/// </summary>
public sealed record NodePushRequest(string NodeId, NodePushKind Kind);
