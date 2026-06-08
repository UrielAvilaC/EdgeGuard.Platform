namespace Dicom.Edge.Hub.Domain.Aggregates.Outbox;

/// <summary>
/// Single source of truth for the well-known outbox topics: the stable keys, their
/// categories, and the default catalog seeded into <c>outbox_topics</c>. Producers and
/// dispatchers reference these keys; the seeder builds the rows from <see cref="Build"/>.
/// </summary>
public static class OutboxTopicCatalog
{
    /// <summary>Topic families used to group the monitoring UI.</summary>
    public static class Categories
    {
        /// <summary>Control-plane pushes to Edge Nodes (config, rules, PACS, equipment).</summary>
        public const string NodeSync = "NodeSync";

        /// <summary>Patient-facing results delivery (Email, WhatsApp).</summary>
        public const string Notification = "Notification";
    }

    // ── Node-sync topics (map 1:1 to NodePushKind) ───────────────────────────
    public const string NodeConfig = "node.config";
    public const string NodeRules = "node.rules";
    public const string NodePacs = "node.pacs";
    public const string NodeEquipment = "node.equipment";

    // ── Notification topics (map 1:1 to NotificationChannel) ─────────────────
    public const string NotificationEmail = "notification.email";
    public const string NotificationWhatsApp = "notification.whatsapp";

    /// <summary>Builds the canonical catalog rows for seeding (idempotent upserts by key).</summary>
    public static IReadOnlyList<OutboxTopic> Build() =>
    [
        OutboxTopic.Create(NodeConfig,        Categories.NodeSync,     "Node Configuration",  "Full configuration snapshot pushed to a node"),
        OutboxTopic.Create(NodeRules,         Categories.NodeSync,     "DICOM Routing Rules", "Per-node DICOM routing rules pushed to a node"),
        OutboxTopic.Create(NodePacs,          Categories.NodeSync,     "PACS Destinations",   "PACS destination assignments pushed to a node"),
        OutboxTopic.Create(NodeEquipment,     Categories.NodeSync,     "Equipment Catalog",   "Equipment catalog with modality codes pushed to a node"),
        OutboxTopic.Create(NotificationEmail,    Categories.Notification, "Email",            "Patient results delivery over Email"),
        OutboxTopic.Create(NotificationWhatsApp, Categories.Notification, "WhatsApp",         "Patient results delivery over WhatsApp"),
    ];
}
