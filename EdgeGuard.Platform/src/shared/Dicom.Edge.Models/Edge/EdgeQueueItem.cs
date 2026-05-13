using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Edge
{
    /// <summary>
    /// Represents a study queued for transmission from Edge Node to Hub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Queue items track studies waiting to be sent to the central Hub.
    /// This enables reliable, asynchronous transfer with retry logic and offline capability.
    /// </para>
    /// <para>
    /// <strong>Queue Lifecycle:</strong>
    /// <code>
    /// Pending → Sending → Completed (success)
    ///                   → Failed (retry or manual intervention)
    /// </code>
    /// </para>
    /// </remarks>
    public class EdgeQueueItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for this queue item.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the Study Instance UID being queued for transfer.
        /// </summary>
        public string StudyInstanceUid { get; set; } = default!;

        /// <summary>
        /// Gets or sets the current transfer status.
        /// </summary>
        public TransferStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the number of transfer retry attempts.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this item was added to the queue.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UTC timestamp of the last transfer attempt.
        /// </summary>
        public DateTime? LastAttempt { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when transfer is scheduled to retry.
        /// </summary>
        /// <remarks>
        /// Uses exponential backoff: 1min, 5min, 15min, 1hr, etc.
        /// </remarks>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>
        /// Gets or sets the priority of this queue item (lower = higher priority).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Priority guidelines:
        /// <list type="bullet">
        ///   <item><description>0 = STAT/Emergency</description></item>
        ///   <item><description>1 = Urgent</description></item>
        ///   <item><description>5 = Routine (default)</description></item>
        ///   <item><description>10 = Background/Batch</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Gets or sets the destination endpoint (Hub URL or AE Title).
        /// </summary>
        public string Destination { get; set; } = default!;

        /// <summary>
        /// Gets or sets the error message from last failed attempt.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the size of the study in bytes.
        /// </summary>
        /// <remarks>
        /// Used for bandwidth estimation and progress tracking.
        /// </remarks>
        public long SizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of instances in this study.
        /// </summary>
        public int InstanceCount { get; set; }

        /// <summary>
        /// Gets or sets whether this item is locked by a worker for processing.
        /// </summary>
        /// <remarks>
        /// Prevents multiple workers from processing the same item concurrently.
        /// </remarks>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the lock was acquired.
        /// </summary>
        public DateTime? LockedAt { get; set; }

        /// <summary>
        /// Gets or sets the worker ID that locked this item.
        /// </summary>
        public string? LockedBy { get; set; }

        /// <summary>
        /// Gets or sets additional context as JSON.
        /// </summary>
        /// <remarks>
        /// Store metadata like patient info, modality, routing decision, etc.
        /// </remarks>
        public string? Metadata { get; set; }

        /// <summary>
        /// Gets whether this item is eligible for retry.
        /// </summary>
        public bool IsEligibleForRetry =>
            Status == TransferStatus.Failed &&
            RetryCount < 10 && // Max 10 retries
            (!NextRetryAt.HasValue || NextRetryAt.Value <= DateTime.UtcNow);

        /// <summary>
        /// Gets the age of this queue item.
        /// </summary>
        public TimeSpan Age => DateTime.UtcNow - CreatedAt;
    }
}
