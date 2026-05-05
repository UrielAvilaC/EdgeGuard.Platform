using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;

/// <summary>
/// Periodic telemetry snapshot reported by an Edge Node.
/// Aggregates association and study-metrics data from the node's local SQLite DB
/// for a specific time window (<see cref="PeriodStart"/> → <see cref="PeriodEnd"/>).
/// </summary>
public sealed class NodeTelemetryRecord : AggregateRoot<string>
{
    public string   NodeId      { get; private set; } = default!;
    public DateTime ReportedAt  { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd   { get; private set; }

    // ── Association aggregates ────────────────────────────────────────────
    public int TotalAssociations    { get; private set; }
    public int AcceptedAssociations { get; private set; }
    public int RejectedAssociations { get; private set; }
    public int AbortedAssociations  { get; private set; }
    public int TotalImagesReceived  { get; private set; }

    // ── Study-metrics aggregates ──────────────────────────────────────────
    public int    CompletedStudies           { get; private set; }
    public long   TotalBytesReceived         { get; private set; }
    public double? AverageReceptionDurationMs { get; private set; }
    public double? AverageThroughputMbps      { get; private set; }

    private NodeTelemetryRecord() { }

    public static NodeTelemetryRecord Create(
        string   nodeId,
        DateTime reportedAt,
        DateTime periodStart,
        DateTime periodEnd,
        int  totalAssociations,
        int  acceptedAssociations,
        int  rejectedAssociations,
        int  abortedAssociations,
        int  totalImagesReceived,
        int  completedStudies,
        long totalBytesReceived,
        double? averageReceptionDurationMs,
        double? averageThroughputMbps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        return new NodeTelemetryRecord
        {
            Id                        = Guid.NewGuid().ToString(),
            NodeId                    = nodeId,
            ReportedAt                = reportedAt,
            PeriodStart               = periodStart,
            PeriodEnd                 = periodEnd,
            TotalAssociations         = totalAssociations,
            AcceptedAssociations      = acceptedAssociations,
            RejectedAssociations      = rejectedAssociations,
            AbortedAssociations       = abortedAssociations,
            TotalImagesReceived       = totalImagesReceived,
            CompletedStudies          = completedStudies,
            TotalBytesReceived        = totalBytesReceived,
            AverageReceptionDurationMs = averageReceptionDurationMs,
            AverageThroughputMbps     = averageThroughputMbps,
        };
    }
}
