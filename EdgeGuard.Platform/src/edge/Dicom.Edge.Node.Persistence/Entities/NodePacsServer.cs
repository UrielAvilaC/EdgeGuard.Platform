namespace Dicom.Edge.Node.Persistence.Entities;

/// <summary>
/// Represents a PACS server assigned to this node by the Hub.
/// Synced from Hub on every configuration push; serves as the authoritative
/// source for PACS host/port used by the routing engine.
/// </summary>
public sealed class NodePacsServer : ITimestampedEntity
{
    /// <summary>Hub PACS server ID — used as the stable identifier for upsert.</summary>
    public string Id { get; set; } = default!;

    /// <summary>Human-readable name (e.g., "PACS-PRINCIPAL").</summary>
    public string Name { get; set; } = default!;

    /// <summary>DICOM AE Title of the destination PACS.</summary>
    public string AeTitle { get; set; } = default!;

    /// <summary>Hostname or IP address of the PACS server.</summary>
    public string Host { get; set; } = default!;

    /// <summary>DICOM port of the PACS server.</summary>
    public int Port { get; set; }

    /// <summary>
    /// Routing priority — lower value = higher priority.
    /// When multiple PACS are assigned, the router processes them in this order.
    /// </summary>
    public int Priority { get; set; } = 10;

    /// <summary>Whether this PACS destination is currently active.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>UTC timestamp of the last Hub sync that wrote this record.</summary>
    public DateTime SyncedAt { get; set; }

    // ── ITimestampedEntity ────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
