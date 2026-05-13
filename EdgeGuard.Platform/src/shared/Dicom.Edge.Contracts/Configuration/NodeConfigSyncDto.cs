namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Payload sent from Hub → Node when pushing key-value configuration settings.
/// PACS destinations are pushed separately via <c>POST /api/pacs-destinations/sync</c>.
/// </summary>
public sealed record NodeConfigSyncDto
{
    /// <summary>SHA-256 hash of sorted key=value pairs. Used for version comparison.</summary>
    public required string ConfigVersion { get; init; }

    /// <summary>UTC timestamp when the Hub computed this payload.</summary>
    public required DateTime GeneratedAtUtc { get; init; }

    /// <summary>Hub-side node identifier.</summary>
    public required string NodeId { get; init; }

    /// <summary>All setting key-value pairs to apply.</summary>
    public required Dictionary<string, string> Settings { get; init; }
}

/// <summary>
/// Payload sent from Hub → Node to synchronize assigned PACS destinations.
/// Sent immediately when a PACS is assigned or unassigned in the Hub.
/// </summary>
public sealed record PacsDestinationsSyncRequest
{
    /// <summary>Hub-side node identifier.</summary>
    public required string NodeId { get; init; }

    /// <summary>UTC timestamp of this sync operation.</summary>
    public required DateTime SyncedAtUtc { get; init; }

    /// <summary>
    /// Complete list of active PACS destinations currently assigned to this node.
    /// The node performs a full replace: records not in this list are removed.
    /// Empty list means all PACS assignments have been removed.
    /// </summary>
    public required IReadOnlyList<PacsDestinationSyncEntry> Destinations { get; init; }
}

/// <summary>
/// Response returned by the node after applying a PACS destinations sync.
/// </summary>
public sealed record PacsDestinationsSyncResponse
{
    public bool Accepted         { get; init; }
    public int  UpsertedCount    { get; init; }
    public int  RemovedCount     { get; init; }
    public DateTime AppliedAtUtc { get; init; }
    public string? Error         { get; init; }
}

/// <summary>
/// A single PACS server destination in a <see cref="PacsDestinationsSyncRequest"/>.
/// </summary>
public sealed record PacsDestinationSyncEntry
{
    public required string Id      { get; init; }
    public required string Name    { get; init; }
    public required string AeTitle { get; init; }
    public required string Host    { get; init; }
    public required int    Port    { get; init; }
    public          int    Priority { get; init; } = 10;
}
