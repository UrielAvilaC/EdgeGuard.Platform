namespace Dicom.Edge.Contracts.Metrics
{
    /// <summary>
    /// Data transfer object for study performance metrics.
    /// </summary>
    public class StudyMetricsDto
    {
        public string Id { get; set; } = default!;
        public string StudyInstanceUid { get; set; } = default!;
        public long TotalSizeBytes { get; set; }
        public double SizeMB { get; set; }
        public double ReceptionDurationSeconds { get; set; }
        public double? TransferDurationSeconds { get; set; }
        public int InstancesReceived { get; set; }
        public int InstancesFailed { get; set; }
        public double AverageImageSizeMB { get; set; }
        public DateTime FirstImageAt { get; set; }
        public DateTime LastImageAt { get; set; }
        public int RetryCount { get; set; }
        public double ReceptionThroughputMbps { get; set; }
        public double? TransferThroughputMbps { get; set; }
        public double SuccessRate { get; set; }
    }
}
