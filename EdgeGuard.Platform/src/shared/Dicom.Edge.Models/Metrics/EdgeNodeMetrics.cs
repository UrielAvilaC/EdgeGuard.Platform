using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Metrics
{
    /// <summary>
    /// Aggregated performance metrics for an Edge Node over a time period.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by the Hub to track Edge Node performance trends and capacity planning.
    /// Typically aggregated hourly, daily, and monthly.
    /// </para>
    /// </remarks>
    public class EdgeNodeMetrics
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the Edge Node ID these metrics apply to.
        /// </summary>
        public string EdgeNodeId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the start of the measurement period (UTC).
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets the end of the measurement period (UTC).
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Gets or sets the metric granularity.
        /// </summary>
        /// <remarks>
        /// "Hourly", "Daily", "Weekly", "Monthly"
        /// </remarks>
        public string Granularity { get; set; } = "Daily";

        /// <summary>
        /// Gets or sets the number of studies received during this period.
        /// </summary>
        public int StudiesReceived { get; set; }

        /// <summary>
        /// Gets or sets the number of studies successfully transferred to Hub.
        /// </summary>
        public int StudiesTransferred { get; set; }

        /// <summary>
        /// Gets or sets the number of failed transfers.
        /// </summary>
        public int TransfersFailed { get; set; }

        /// <summary>
        /// Gets or sets the total number of DICOM instances received.
        /// </summary>
        public int TotalInstances { get; set; }

        /// <summary>
        /// Gets or sets the total data volume in bytes.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets the average transfer time in seconds.
        /// </summary>
        public double AverageTransferTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the average throughput in megabits per second.
        /// </summary>
        public double AverageThroughputMbps { get; set; }

        /// <summary>
        /// Gets or sets the peak throughput achieved.
        /// </summary>
        public double PeakThroughputMbps { get; set; }

        /// <summary>
        /// Gets or sets the average CPU usage percentage.
        /// </summary>
        public double AverageCpuUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets the peak CPU usage.
        /// </summary>
        public double PeakCpuUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets the average memory usage in megabytes.
        /// </summary>
        public double AverageMemoryUsageMb { get; set; }

        /// <summary>
        /// Gets or sets the average storage usage percentage.
        /// </summary>
        public double AverageStorageUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets the number of errors logged.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the number of associations established.
        /// </summary>
        public int AssociationsEstablished { get; set; }

        /// <summary>
        /// Gets or sets the number of associations rejected.
        /// </summary>
        public int AssociationsRejected { get; set; }

        /// <summary>
        /// Gets or sets the average queue size.
        /// </summary>
        public double AverageQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the peak queue size.
        /// </summary>
        public int PeakQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the uptime percentage.
        /// </summary>
        /// <remarks>
        /// Percentage of time the node was online during this period.
        /// </remarks>
        public double UptimePercent { get; set; }

        /// <summary>
        /// Gets or sets the number of minutes the node was offline.
        /// </summary>
        public int DowntimeMinutes { get; set; }

        /// <summary>
        /// Gets the transfer success rate.
        /// </summary>
        public double TransferSuccessRate =>
            StudiesReceived > 0
                ? (StudiesTransferred / (double)StudiesReceived) * 100
                : 100;

        /// <summary>
        /// Gets the association acceptance rate.
        /// </summary>
        public double AssociationAcceptanceRate =>
            AssociationsEstablished + AssociationsRejected > 0
                ? (AssociationsEstablished / (double)(AssociationsEstablished + AssociationsRejected)) * 100
                : 100;

        /// <summary>
        /// Gets the average data per study in megabytes.
        /// </summary>
        public double AverageStudySizeMb =>
            StudiesReceived > 0
                ? (TotalBytes / (1024.0 * 1024.0)) / StudiesReceived
                : 0;
    }
}
