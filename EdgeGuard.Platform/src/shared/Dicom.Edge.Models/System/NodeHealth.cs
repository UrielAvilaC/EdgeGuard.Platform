using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.System
{
    /// <summary>
    /// Real-time health and performance status of an Edge Node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Health snapshots enable:
    /// <list type="bullet">
    ///   <item><description>Real-time monitoring dashboards</description></item>
    ///   <item><description>Alerting on resource exhaustion</description></item>
    ///   <item><description>Capacity planning</description></item>
    ///   <item><description>Automated failover decisions</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Recommended collection frequency: Every 30-60 seconds.
    /// </para>
    /// </remarks>
    public class NodeHealth
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string NodeId { get; set; } = default!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public NodeStatus Status { get; set; }
        
        /// <summary>
        /// Available storage in megabytes on the DICOM storage volume.
        /// </summary>
        public long AvailableStorageMb { get; set; }
        
        /// <summary>
        /// Total storage capacity in megabytes.
        /// </summary>
        public long TotalStorageMb { get; set; }
        
        /// <summary>
        /// Number of active DICOM associations.
        /// </summary>
        public int ActiveConnections { get; set; }
        
        /// <summary>
        /// Number of studies waiting in queue for processing/transfer.
        /// </summary>
        public int QueuedStudies { get; set; }
        
        /// <summary>
        /// CPU usage percentage (0-100).
        /// </summary>
        public double CpuUsagePercent { get; set; }
        
        /// <summary>
        /// Memory usage in megabytes.
        /// </summary>
        public double MemoryUsageMb { get; set; }
        
        /// <summary>
        /// Total available memory in megabytes.
        /// </summary>
        public double TotalMemoryMb { get; set; }
        
        /// <summary>
        /// Network throughput in megabits per second.
        /// </summary>
        public double NetworkThroughputMbps { get; set; }
        
        /// <summary>
        /// Number of studies received in the last hour.
        /// </summary>
        public int StudiesReceivedLastHour { get; set; }
        
        /// <summary>
        /// Number of errors in the last hour.
        /// </summary>
        public int ErrorsLastHour { get; set; }
        
        /// <summary>
        /// Error message if status is Degraded or Offline.
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Software version running on this node.
        /// </summary>
        public string? SoftwareVersion { get; set; }
        
        /// <summary>
        /// Gets storage usage percentage.
        /// </summary>
        public double StorageUsagePercent =>
            TotalStorageMb > 0
                ? ((TotalStorageMb - AvailableStorageMb) / (double)TotalStorageMb) * 100
                : 0;
        
        /// <summary>
        /// Gets memory usage percentage.
        /// </summary>
        public double MemoryUsagePercent =>
            TotalMemoryMb > 0
                ? (MemoryUsageMb / TotalMemoryMb) * 100
                : 0;
        
        /// <summary>
        /// Determines if node is healthy based on thresholds.
        /// </summary>
        public bool IsHealthy =>
            Status == NodeStatus.Online &&
            StorageUsagePercent < 90 &&
            CpuUsagePercent < 90 &&
            MemoryUsagePercent < 90 &&
            ErrorsLastHour < 10;
    }
}
