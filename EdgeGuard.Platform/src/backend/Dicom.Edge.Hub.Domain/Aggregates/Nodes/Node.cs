using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes.Events;
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

        node.AddDomainEvent(new NodeRegisteredEvent(node.Id, name, aeTitle.Value));
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
}
