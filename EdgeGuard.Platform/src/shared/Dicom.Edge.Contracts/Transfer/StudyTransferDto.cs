namespace Dicom.Edge.Contracts.Transfer
{
    /// <summary>
    /// Data transfer object for study transfer tracking.
    /// </summary>
    public class StudyTransferDto
    {
        public string Id { get; set; } = default!;
        public string StudyInstanceUid { get; set; } = default!;
        public string EdgeNodeId { get; set; } = default!;
        public string Status { get; set; } = default!;
        
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Progress
        public long TotalSizeBytes { get; set; }
        public long BytesTransferred { get; set; }
        public double ProgressPercent { get; set; }
        
        public int TotalInstances { get; set; }
        public int InstancesTransferred { get; set; }
        public int InstancesFailed { get; set; }
        
        // Metrics
        public double? DurationSeconds { get; set; }
        public double? ThroughputMbps { get; set; }
        public double InstanceSuccessRate { get; set; }
        
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TransferMethod { get; set; }
        
        // Source info
        public string? SourceAeTitle { get; set; }
        public string? PatientId { get; set; }
        public DateTime? StudyDate { get; set; }
        public string? Modality { get; set; }
        public int Priority { get; set; }
        
        public bool IsSuccess { get; set; }
    }
}
