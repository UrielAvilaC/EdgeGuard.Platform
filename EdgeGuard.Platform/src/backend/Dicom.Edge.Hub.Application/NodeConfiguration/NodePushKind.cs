using Dicom.Edge.Hub.Domain.Aggregates.Outbox;

namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Kind of configuration push to dispatch to an Edge Node. Maps 1:1 to a <c>node.*</c>
/// outbox topic and to the corresponding push service in the dispatcher.
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

/// <summary>Maps a <see cref="NodePushKind"/> to its <c>outbox_topics</c> key.</summary>
public static class NodePushKindExtensions
{
    public static string ToTopicId(this NodePushKind kind) => kind switch
    {
        NodePushKind.Config => OutboxTopicCatalog.NodeConfig,
        NodePushKind.Rules => OutboxTopicCatalog.NodeRules,
        NodePushKind.Pacs => OutboxTopicCatalog.NodePacs,
        NodePushKind.Equipment => OutboxTopicCatalog.NodeEquipment,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown node push kind")
    };
}
