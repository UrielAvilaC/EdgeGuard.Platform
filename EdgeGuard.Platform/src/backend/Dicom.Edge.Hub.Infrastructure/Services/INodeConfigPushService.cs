using Dicom.Edge.Contracts.Configuration;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Pushes configuration snapshots from the Hub to a target Edge Node via HTTP.
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
