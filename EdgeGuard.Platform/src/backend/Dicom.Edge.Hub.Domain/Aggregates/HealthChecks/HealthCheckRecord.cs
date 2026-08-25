using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks.Events;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;

/// <summary>
/// HealthCheck record aggregate. Stores a snapshot of a node's health at a point in time.
/// </summary>
public sealed class HealthCheckRecord : AggregateRoot<string>
{
    private readonly List<PacsCEchoResult> _pacsResults = [];

    public string NodeId { get; private set; } = default!;
    public DateTime ReceivedAt { get; private set; }
    public NodeStatus ReportedNodeStatus { get; private set; }

    // System metrics
    public double? CpuUsagePercent { get; private set; }
    public long? MemoryUsageMb { get; private set; }
    /// <summary>
    /// Peso del workspace al momento de la medición: estudios vivos y base del
    /// nodo por separado. Antes esto era espacio libre; se invirtió al modelo de
    /// espacio usado porque el libre no significa nada sin conocer la capacidad.
    /// </summary>
    public long? StorageDicomMb { get; private set; }
    public long? StorageDatabaseMb { get; private set; }
    public long? StorageVolumeFreeMb { get; private set; }
    public long? StorageLimitMb { get; private set; }
    public int? ActiveAssociations { get; private set; }
    public int? QueuedStudies { get; private set; }
    public long? UptimeSeconds { get; private set; }

    // Network
    public double? LatencyMs { get; private set; }
    public double? BandwidthMbps { get; private set; }

    public IReadOnlyList<PacsCEchoResult> PacsResults => _pacsResults.AsReadOnly();

    private HealthCheckRecord() { }

    public static HealthCheckRecord Create(
        string nodeId,
        NodeStatus reportedStatus,
        double? cpuUsagePercent = null,
        long? memoryUsageMb = null,
        long? storageDicomMb = null,
        long? storageDatabaseMb = null,
        long? storageVolumeFreeMb = null,
        long? storageLimitMb = null,
        int? activeAssociations = null,
        int? queuedStudies = null,
        long? uptimeSeconds = null,
        double? latencyMs = null,
        double? bandwidthMbps = null)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node ID cannot be empty.", nameof(nodeId));

        var record = new HealthCheckRecord
        {
            Id = IdGenerator.NewId(),
            NodeId = nodeId,
            ReceivedAt = DateTime.UtcNow,
            ReportedNodeStatus = reportedStatus,
            CpuUsagePercent = cpuUsagePercent,
            MemoryUsageMb = memoryUsageMb,
            StorageDicomMb = storageDicomMb,
            StorageDatabaseMb = storageDatabaseMb,
            StorageVolumeFreeMb = storageVolumeFreeMb,
            StorageLimitMb = storageLimitMb,
            ActiveAssociations = activeAssociations,
            QueuedStudies = queuedStudies,
            UptimeSeconds = uptimeSeconds,
            LatencyMs = latencyMs,
            BandwidthMbps = bandwidthMbps
        };

        record.AddDomainEvent(new HealthCheckReceivedEvent(nodeId, record.Id));
        return record;
    }

    public void AddPacsCEchoResult(PacsCEchoResult result)
    {
        _pacsResults.Add(result);
    }
}
