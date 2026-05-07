namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Pushes configuration snapshots from the Hub to a target Edge Node via HTTP.
/// Interface lives in Application layer (DIP); implementation in Infrastructure.
/// </summary>
public interface INodeConfigPushService
{
    /// <summary>
    /// Sends the full configuration payload to the specified node and returns the result.
    /// </summary>
    Task<ConfigPushResult> PushConfigAsync(
        string nodeId, CancellationToken ct = default);
}

/// <summary>
/// Pushes the current PACS destination assignments to a node immediately after
/// any assign/unassign operation. Separate from the general config push so PACS
/// changes don't require a full settings snapshot.
/// </summary>
public interface INodePacsDestinationPushService
{
    /// <summary>
    /// Resolves all active PACS assignments for <paramref name="nodeId"/> and
    /// sends them to the node via <c>POST /api/pacs-destinations/sync</c>.
    /// Returns false if the node has no API endpoint or the push fails.
    /// </summary>
    Task<bool> PushAsync(string nodeId, CancellationToken ct = default);
}

/// <summary>
/// Outcome of a configuration push attempt.
/// </summary>
public sealed class ConfigPushResult
{
    public bool Success { get; init; }
    public string? AppliedVersion { get; init; }
    public int UpdatedCount { get; init; }
    public string? Error { get; init; }

    public static ConfigPushResult Ok(string appliedVersion, int updatedCount) => new()
    {
        Success = true,
        AppliedVersion = appliedVersion,
        UpdatedCount = updatedCount
    };

    public static ConfigPushResult Fail(string error) => new()
    {
        Success = false,
        Error = error
    };
}
