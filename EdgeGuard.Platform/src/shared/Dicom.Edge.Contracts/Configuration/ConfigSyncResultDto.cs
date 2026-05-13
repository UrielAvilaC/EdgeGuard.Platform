namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Response returned by the Node after receiving a configuration push.
/// </summary>
public sealed class ConfigSyncResultDto
{
    /// <summary>Whether the node accepted and applied the configuration.</summary>
    public required bool Accepted { get; init; }

    /// <summary>The config version the node now reports after applying.</summary>
    public string? AppliedVersion { get; init; }

    /// <summary>Number of settings that were actually updated.</summary>
    public int UpdatedCount { get; init; }

    /// <summary>Error message if the push was rejected.</summary>
    public string? Error { get; init; }

    /// <summary>UTC timestamp when the node applied the config.</summary>
    public DateTime AppliedAtUtc { get; init; }
}
