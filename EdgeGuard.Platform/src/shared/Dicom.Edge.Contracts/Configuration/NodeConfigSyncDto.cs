namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Payload sent from Hub → Node when pushing configuration.
/// Contains all settings the Hub considers current for this node.
/// </summary>
public sealed class NodeConfigSyncDto
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
