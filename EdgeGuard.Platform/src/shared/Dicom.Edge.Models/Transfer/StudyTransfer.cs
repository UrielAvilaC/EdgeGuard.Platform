using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Transfer
{
    /// <summary>
    /// Represents a study transfer operation from an Edge Node to the Hub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model tracks the complete lifecycle of a study being transmitted from
    /// an Edge Node to the central Hub, including performance metrics and error tracking.
    /// </para>
    /// <para>
    /// <strong>Transfer Lifecycle:</strong>
    /// <code>
    /// Pending → Sending → Completed (success)
    ///                   → Failed (with retry logic)
    /// </code>
    /// </para>
    /// </remarks>
    public class StudyTransfer
    {
        /// <summary>
        /// Gets or sets the unique identifier for this transfer operation.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the Study Instance UID being transferred.
        /// </summary>
        public string StudyInstanceUid { get; set; } = default!;

        /// <summary>
        /// Gets or sets the ID of the Edge Node sending the study.
        /// </summary>
        public string EdgeNodeId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the current transfer status.
        /// </summary>
        public TransferStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the transfer started.
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UTC timestamp when the transfer completed (successfully or with failure).
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the total size of the study in bytes.
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes successfully transferred.
        /// </summary>
        public long BytesTransferred { get; set; }

        /// <summary>
        /// Gets or sets the total number of DICOM instances in the study.
        /// </summary>
        public int TotalInstances { get; set; }

        /// <summary>
        /// Gets or sets the number of instances successfully transferred.
        /// </summary>
        public int InstancesTransferred { get; set; }

        /// <summary>
        /// Gets or sets the number of instances that failed during transfer.
        /// </summary>
        public int InstancesFailed { get; set; }

        /// <summary>
        /// Gets or sets the number of retry attempts for this transfer.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the error message if transfer failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the transfer method used.
        /// </summary>
        /// <remarks>
        /// Examples: "DICOM C-STORE", "HTTP POST", "HTTPS Multipart"
        /// </remarks>
        public string? TransferMethod { get; set; }

        /// <summary>
        /// Gets or sets the source AE Title of the original modality.
        /// </summary>
        /// <remarks>
        /// Preserved from the Edge Node for traceability.
        /// </remarks>
        public string? SourceAeTitle { get; set; }

        /// <summary>
        /// Gets or sets the patient ID for quick lookups.
        /// </summary>
        public string? PatientId { get; set; }

        /// <summary>
        /// Gets or sets the study date for reporting.
        /// </summary>
        public DateTime? StudyDate { get; set; }

        /// <summary>
        /// Gets or sets the modality type.
        /// </summary>
        /// <remarks>
        /// Examples: "CT", "MR", "CR", "US"
        /// </remarks>
        public string? Modality { get; set; }

        /// <summary>
        /// Gets or sets the priority of this transfer.
        /// </summary>
        /// <remarks>
        /// 0 = STAT, 1 = Urgent, 5 = Routine, 10 = Background
        /// </remarks>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether this transfer was completed successfully.
        /// </summary>
        public bool IsSuccess => Status == TransferStatus.Completed;

        /// <summary>
        /// Gets the transfer progress percentage.
        /// </summary>
        public double ProgressPercent =>
            TotalSizeBytes > 0
                ? (BytesTransferred / (double)TotalSizeBytes) * 100
                : 0;

        /// <summary>
        /// Gets the transfer duration.
        /// </summary>
        public TimeSpan? Duration =>
            CompletedAt.HasValue
                ? CompletedAt.Value - StartedAt
                : null;

        /// <summary>
        /// Gets the average transfer throughput in megabits per second.
        /// </summary>
        public double? ThroughputMbps
        {
            get
            {
                if (!Duration.HasValue || Duration.Value.TotalSeconds == 0)
                    return null;

                return (BytesTransferred * 8 / (1_000_000.0)) / Duration.Value.TotalSeconds;
            }
        }

        /// <summary>
        /// Gets the instance success rate.
        /// </summary>
        public double InstanceSuccessRate =>
            TotalInstances > 0
                ? (InstancesTransferred / (double)TotalInstances) * 100
                : 0;
    }
}
