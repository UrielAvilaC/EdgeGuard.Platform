namespace Dicom.Edge.Contracts.Monitoring
{
    /// <summary>
    /// Data transfer object for real-time node health status.
    /// </summary>
    public class NodeHealthDto
    {
        public string Id { get; set; } = default!;
        public string NodeId { get; set; } = default!;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = default!;
        
        // Storage
        public long AvailableStorageMb { get; set; }
        public long TotalStorageMb { get; set; }
        public double StorageUsagePercent { get; set; }
        
        // Resources
        public double CpuUsagePercent { get; set; }
        public double MemoryUsageMb { get; set; }
        public double TotalMemoryMb { get; set; }
        public double MemoryUsagePercent { get; set; }
        
        // Operations
        public int ActiveConnections { get; set; }
        public int QueuedStudies { get; set; }
        public int StudiesReceivedLastHour { get; set; }
        public int ErrorsLastHour { get; set; }
        
        // Network
        public double NetworkThroughputMbps { get; set; }
        
        public string? ErrorMessage { get; set; }
        public bool IsHealthy { get; set; }
    }
}
