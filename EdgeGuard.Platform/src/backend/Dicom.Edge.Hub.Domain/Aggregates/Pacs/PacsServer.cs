using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs.Events;

namespace Dicom.Edge.Hub.Domain.Aggregates.Pacs;

/// <summary>
/// PACS server aggregate. Managed centrally by the Hub and optionally inherited to nodes.
/// </summary>
public sealed class PacsServer : AggregateRoot<string>
{
    public string Name { get; private set; } = default!;
    public AeTitle AeTitle { get; private set; } = default!;
    public string HostName { get; private set; } = default!;
    public int Port { get; private set; }
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public int MaxConcurrentAssociations { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public DateTime? LastCEchoAt { get; private set; }
    public bool LastCEchoSuccess { get; private set; }
    public bool IsReachable { get; private set; }

    /// <summary>
    /// When true, this PACS is automatically inherited by all nodes.
    /// </summary>
    public bool IsGlobal { get; private set; }

    public string? SupportedModalitiesCsv { get; private set; }

    private PacsServer() { }

    public static PacsServer Create(
        string name,
        AeTitle aeTitle,
        string hostName,
        int port,
        string? description = null,
        bool isGlobal = false,
        int maxConcurrentAssociations = 10,
        int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("PACS name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(hostName))
            throw new ArgumentException("Host name cannot be empty.", nameof(hostName));
        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        var pacs = new PacsServer
        {
            Id = IdGenerator.NewId(),
            Name = name.Trim(),
            AeTitle = aeTitle,
            HostName = hostName.Trim(),
            Port = port,
            Description = description?.Trim(),
            IsEnabled = true,
            IsGlobal = isGlobal,
            MaxConcurrentAssociations = maxConcurrentAssociations,
            TimeoutSeconds = timeoutSeconds,
            IsReachable = false,
            LastCEchoSuccess = false
        };

        pacs.AddDomainEvent(new PacsRegisteredEvent(pacs.Id, aeTitle.Value, hostName, port));
        return pacs;
    }

    public void UpdateCEchoStatus(bool success, DateTime timestamp)
    {
        LastCEchoAt = timestamp;
        LastCEchoSuccess = success;
        IsReachable = success;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PacsCEchoResultEvent(Id, AeTitle.Value, success));
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PacsStatusChangedEvent(Id, AeTitle.Value, IsEnabled: true));
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PacsStatusChangedEvent(Id, AeTitle.Value, IsEnabled: false));
    }

    public void MarkAsGlobal()
    {
        IsGlobal = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsLocal()
    {
        IsGlobal = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateConfiguration(
        string name,
        string hostName,
        int port,
        string? description,
        int maxConcurrentAssociations,
        int timeoutSeconds)
    {
        Name = name.Trim();
        HostName = hostName.Trim();
        Port = port;
        Description = description?.Trim();
        MaxConcurrentAssociations = maxConcurrentAssociations;
        TimeoutSeconds = timeoutSeconds;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSupportedModalities(IEnumerable<string> modalities)
    {
        SupportedModalitiesCsv = string.Join(",", modalities.Select(m => m.Trim().ToUpperInvariant()));
        UpdatedAt = DateTime.UtcNow;
    }
}
