namespace Dicom.Edge.Node.Persistence.Entities;

/// <summary>
/// Tracks the registration state of this Edge Node with the central Hub.
/// Only one row should ever exist (singleton pattern enforced in seed).
/// </summary>
public sealed class NodeRegistrationEntity : ITimestampedEntity
{
    /// <summary>Local primary key (GUID).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Node ID assigned by the Hub after successful registration. Null before first registration.</summary>
    public string? HubNodeId { get; set; }

    /// <summary>
    /// Registration state machine value.
    /// Valid values: "pending" | "registered" | "rejected" | "deregistered"
    /// </summary>
    public string RegistrationStatus { get; set; } = "pending";

    /// <summary>Effective Hub base URL used during last successful communication.</summary>
    public string? HubBaseUrl { get; set; }

    /// <summary>Software version acknowledged by the Hub for this node.</summary>
    public string? HubAssignedVersion { get; set; }

    /// <summary>Last error message from Hub communication, if any.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>UTC timestamp of successful registration with Hub.</summary>
    public DateTime? RegisteredAt { get; set; }

    /// <summary>UTC timestamp of last successful configuration pull from Hub.</summary>
    public DateTime? LastConfigSyncAt { get; set; }

    /// <summary>UTC timestamp of last successful heartbeat sent to Hub.</summary>
    public DateTime? LastHeartbeatAt { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime UpdatedAt { get; set; }
}
