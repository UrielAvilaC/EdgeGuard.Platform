using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Distribution
{
    /// <summary>
    /// Represents the distribution of a study from the Hub to destination PACS systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After receiving studies from Edge Nodes, the Hub may distribute them to multiple
    /// destination PACS based on routing rules (e.g., main archive, specialty PACS, research archive).
    /// </para>
    /// </remarks>
    public class StudyDistribution
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the Study Instance UID being distributed.
        /// </summary>
        public string StudyInstanceUid { get; set; } = default!;

        /// <summary>
        /// Gets or sets the Edge Node ID that originally sent this study.
        /// </summary>
        public string? SourceEdgeNodeId { get; set; }

        /// <summary>
        /// Gets or sets the destination PACS AE Title.
        /// </summary>
        public string DestinationAeTitle { get; set; } = default!;

        /// <summary>
        /// Gets or sets the destination PACS IP address.
        /// </summary>
        public string DestinationIpAddress { get; set; } = default!;

        /// <summary>
        /// Gets or sets the destination PACS port.
        /// </summary>
        public int DestinationPort { get; set; } = 104;

        /// <summary>
        /// Gets or sets the distribution status.
        /// </summary>
        public TransferStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when distribution started.
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UTC timestamp when distribution completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of instances successfully sent.
        /// </summary>
        public int InstancesSent { get; set; }

        /// <summary>
        /// Gets or sets the total number of instances to send.
        /// </summary>
        public int TotalInstances { get; set; }

        /// <summary>
        /// Gets or sets the number of retry attempts.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the error message if distribution failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the priority of this distribution.
        /// </summary>
        /// <remarks>
        /// 0 = STAT, 1 = Urgent, 5 = Routine, 10 = Background
        /// </remarks>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Gets or sets the routing rule ID that triggered this distribution.
        /// </summary>
        public string? RoutingRuleId { get; set; }

        /// <summary>
        /// Gets or sets whether data was anonymized before sending.
        /// </summary>
        public bool WasAnonymized { get; set; }

        /// <summary>
        /// Gets the distribution progress percentage.
        /// </summary>
        public double ProgressPercent =>
            TotalInstances > 0
                ? (InstancesSent / (double)TotalInstances) * 100
                : 0;

        /// <summary>
        /// Gets the distribution duration.
        /// </summary>
        public TimeSpan? Duration =>
            CompletedAt.HasValue
                ? CompletedAt.Value - StartedAt
                : null;

        /// <summary>
        /// Gets whether distribution is complete and successful.
        /// </summary>
        public bool IsSuccess => Status == TransferStatus.Completed && InstancesSent == TotalInstances;
    }
}
