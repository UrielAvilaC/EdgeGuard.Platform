namespace Dicom.Edge.Models.Metrics
{
    /// <summary>
    /// Performance and operational metrics for a completed DICOM study transfer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Metrics enable:
    /// <list type="bullet">
    ///   <item><description>Performance monitoring and SLA tracking</description></item>
    ///   <item><description>Capacity planning and resource optimization</description></item>
    ///   <item><description>Identifying network or system bottlenecks</description></item>
    ///   <item><description>Quality assurance and anomaly detection</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class StudyMetrics
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StudyInstanceUid { get; set; } = default!;
        
        /// <summary>
        /// Total size of all instances in bytes.
        /// </summary>
        public long TotalSizeBytes { get; set; }
        
        /// <summary>
        /// Time taken to receive all instances from modality.
        /// </summary>
        public TimeSpan ReceptionDuration { get; set; }
        
        /// <summary>
        /// Time taken to send study to destination (Hub/PACS).
        /// </summary>
        public TimeSpan? TransferDuration { get; set; }
        
        /// <summary>
        /// Number of instances successfully received.
        /// </summary>
        public int InstancesReceived { get; set; }
        
        /// <summary>
        /// Number of instances that failed during reception.
        /// </summary>
        public int InstancesFailed { get; set; }
        
        /// <summary>
        /// Average size per instance in bytes.
        /// </summary>
        public double AverageImageSize { get; set; }
        
        /// <summary>
        /// UTC timestamp when first instance arrived.
        /// </summary>
        public DateTime FirstImageAt { get; set; }
        
        /// <summary>
        /// UTC timestamp when last instance arrived.
        /// </summary>
        public DateTime LastImageAt { get; set; }
        
        /// <summary>
        /// Number of retry attempts needed for successful transfer.
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// Reception throughput in MB/second.
        /// </summary>
        public double ReceptionThroughputMbps =>
            ReceptionDuration.TotalSeconds > 0
                ? (TotalSizeBytes / (1024.0 * 1024.0)) / ReceptionDuration.TotalSeconds
                : 0;
        
        /// <summary>
        /// Transfer throughput in MB/second.
        /// </summary>
        public double? TransferThroughputMbps =>
            TransferDuration?.TotalSeconds > 0
                ? (TotalSizeBytes / (1024.0 * 1024.0)) / TransferDuration.Value.TotalSeconds
                : null;
        
        /// <summary>
        /// Success rate (instances received / total expected).
        /// </summary>
        public double SuccessRate =>
            InstancesReceived + InstancesFailed > 0
                ? (double)InstancesReceived / (InstancesReceived + InstancesFailed)
                : 0;
    }
}
