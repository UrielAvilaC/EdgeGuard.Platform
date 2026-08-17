using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes.Events;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes;

/// <summary>
/// Edge Node aggregate root. Represents a node registered in the Hub.
/// </summary>
public sealed class Node : AggregateRoot<string>, ISoftDeletable
{
    private readonly List<NodePacsAssignment> _pacsAssignments = [];

    public string Name { get; private set; } = default!;
    public AeTitle AeTitle { get; private set; } = default!;
    public string IpAddress { get; private set; } = default!;
    public int Port { get; private set; }
    public string? ApiEndpoint { get; private set; }
    public string? Location { get; private set; }
    public string? FacilityName { get; private set; }
    public string? TimeZone { get; private set; }
    public string? Version { get; private set; }
    public NodeStatus Status { get; private set; }
    public DateTime? LastHeartbeatAt { get; private set; }
    public bool IsEnabled { get; private set; }

    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// BCrypt hash of the node's API key. Set once during registration.
    /// </summary>
    public string? ApiKeyHash { get; private set; }

    /// <summary>
    /// The same API key as <see cref="ApiKeyHash"/>, kept in reversible form and encrypted
    /// at rest (Data Protection ciphertext, <c>ENC:</c> prefix).
    ///
    /// <para>A hash only answers "is this the right key?", which is all an INBOUND node→Hub
    /// call needs. Signing an OUTBOUND Hub→Node request needs the key itself, because the node
    /// verifies an HMAC computed with it. Both fields therefore always describe the same key
    /// and must be written together — see <see cref="SetApiKey"/>.</para>
    /// </summary>
    public string? SigningSecret { get; private set; }

    /// <summary>
    /// Configurable healthcheck interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; private set; }

    // Storage
    public long MaxStorageMb { get; private set; }
    public long AvailableStorageMb { get; private set; }

    // Metrics
    public int TotalStudiesReceived { get; private set; }
    public int TotalStudiesSent { get; private set; }
    public int ErrorsLast24Hours { get; private set; }

    public IReadOnlyList<NodePacsAssignment> PacsAssignments => _pacsAssignments.AsReadOnly();

    private Node() { }

    public static Node Create(
        string name,
        AeTitle aeTitle,
        string ipAddress,
        int port,
        string? apiEndpoint = null,
        string? location = null,
        string? facilityName = null,
        int healthCheckIntervalSeconds = 60)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Node name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be empty.", nameof(ipAddress));
        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        var node = new Node
        {
            Id = IdGenerator.NewId(),
            Name = name.Trim(),
            AeTitle = aeTitle,
            IpAddress = ipAddress.Trim(),
            Port = port,
            ApiEndpoint = apiEndpoint?.Trim(),
            Location = location?.Trim(),
            FacilityName = facilityName?.Trim(),
            Status = NodeStatus.Offline,
            IsEnabled = true,
            HealthCheckIntervalSeconds = healthCheckIntervalSeconds,
        };

        node.AddDomainEvent(new NodeRegisteredEvent(node.Id, name));
        return node;
    }

    public void UpdateHeartbeat(
        long? availableStorageMb = null,
        int? totalStudiesReceived = null,
        int? totalStudiesSent = null,
        int? errorsLast24Hours = null)
    {
        LastHeartbeatAt = DateTime.UtcNow;
        if (availableStorageMb.HasValue) AvailableStorageMb = availableStorageMb.Value;
        if (totalStudiesReceived.HasValue) TotalStudiesReceived = totalStudiesReceived.Value;
        if (totalStudiesSent.HasValue) TotalStudiesSent = totalStudiesSent.Value;
        if (errorsLast24Hours.HasValue) ErrorsLast24Hours = errorsLast24Hours.Value;
        UpdatedAt = DateTime.UtcNow;

        if (Status == NodeStatus.Offline)
            MarkOnline();

        AddDomainEvent(new NodeHeartbeatReceivedEvent(Id, DateTime.UtcNow));
    }

    public void MarkOnline()
    {
        var old = Status;
        Status = NodeStatus.Online;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new NodeStatusChangedEvent(Id, old, Status));
    }

    public void MarkOffline()
    {
        var old = Status;
        Status = NodeStatus.Offline;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new NodeStatusChangedEvent(Id, old, Status));
    }

    public void MarkDegraded()
    {
        var old = Status;
        Status = NodeStatus.Degraded;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new NodeStatusChangedEvent(Id, old, Status));
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignPacs(string pacsId, bool inheritedFromHub = false, int cEchoIntervalSeconds = 300)
    {
        if (_pacsAssignments.Any(a => a.PacsId == pacsId && a.IsActive))
            return;

        var assignment = NodePacsAssignment.Create(Id, pacsId, inheritedFromHub, cEchoIntervalSeconds);
        _pacsAssignments.Add(assignment);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PacsAssignedToNodeEvent(Id, pacsId, inheritedFromHub));
    }

    public void UnassignPacs(string pacsId)
    {
        var assignment = _pacsAssignments.FirstOrDefault(a => a.PacsId == pacsId && a.IsActive);
        if (assignment is null) return;

        assignment.Deactivate();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PacsUnassignedFromNodeEvent(Id, pacsId));
    }

    public void UpdateConfiguration(
        string? location = null,
        string? facilityName = null,
        string? timeZone = null,
        string? version = null,
        int? healthCheckIntervalSeconds = null,
        long? maxStorageMb = null)
    {
        if (location is not null) Location = location.Trim();
        if (facilityName is not null) FacilityName = facilityName.Trim();
        if (timeZone is not null) TimeZone = timeZone.Trim();
        if (version is not null) Version = version.Trim();
        if (healthCheckIntervalSeconds.HasValue) HealthCheckIntervalSeconds = healthCheckIntervalSeconds.Value;
        if (maxStorageMb.HasValue) MaxStorageMb = maxStorageMb.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the API key hash. Can only be set once (immutable after first registration).
    /// </summary>
    public void SetApiKeyHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("API key hash cannot be empty.", nameof(hash));

        ApiKeyHash = hash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Stores both representations of the node's API key: the BCrypt hash used to verify
    /// inbound calls, and the encrypted key used to sign outbound pushes. Callers pass the
    /// ciphertext, never the raw key — the aggregate does not know how to encrypt.
    /// </summary>
    public void SetApiKey(string hash, string encryptedSecret)
    {
        SetApiKeyHash(hash);
        SetSigningSecret(encryptedSecret);
    }

    /// <summary>
    /// Stores the encrypted API key on its own. Used to backfill nodes that registered before
    /// <see cref="SigningSecret"/> existed: their raw key cannot be recovered from the hash, so
    /// it is captured the next time the node presents it on an authenticated call.
    /// </summary>
    public void SetSigningSecret(string encryptedSecret)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret))
            throw new ArgumentException("Signing secret cannot be empty.", nameof(encryptedSecret));

        SigningSecret = encryptedSecret;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the node has an API key assigned.
    /// </summary>
    public bool HasApiKey => !string.IsNullOrEmpty(ApiKeyHash);

    /// <summary>
    /// Returns true if the Hub can sign outbound requests to this node.
    /// </summary>
    public bool HasSigningSecret => !string.IsNullOrEmpty(SigningSecret);
}
